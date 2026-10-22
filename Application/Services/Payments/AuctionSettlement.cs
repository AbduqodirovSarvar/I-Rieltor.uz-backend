using Application.Exceptions;
using Application.Interfaces.Notifications;
using Application.Interfaces.Payments;
using Application.Interfaces.Repositories;
using Application.Notifications;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services.Payments
{
    public sealed class AuctionSettlement(
        IAuctionRepository auctions,
        IAuctionParticipantRepository participants,
        IBidRepository bids,
        IAuctionResultRepository results,
        IAuctionAwardRepository awards,
        IPropertyRepository properties,
        IPaymentRepository payments,
        IDepositRefundService refunds,
        INotificationService notifications,
        IConfiguration configuration,
        ILogger<AuctionSettlement> logger) : IAuctionSettlement
    {
        /// <summary>Marks a refund that returns money which arrived when it could not be used (not the participant's own deposit).</summary>
        private const string ExtraPaymentNote = "Qaytarish: to'lov ";

        private int FinalPaymentDays => configuration.GetValue("Auctions:FinalPaymentDays", 5);
        private int RunnerUpResponseDays => configuration.GetValue("Auctions:RunnerUpResponseDays", 2);

        // ---------------- after the bidding

        public async Task AfterFinishAsync(Auction auction, AuctionAward? winnerAward, CancellationToken cancellationToken = default)
        {
            if (winnerAward == null)
            {
                await ReleaseDepositsAsync(auction, [], "Savdo o'tmadi", cancellationToken);
                return;
            }

            // The runner-up's deposit is held until the winner has paid: if the winner does not, the property is offered to them.
            var runnerUp = auction.OfferToRunnerUp ? await FindRunnerUpAsync(auction.Id, winnerAward.ParticipantId, cancellationToken) : null;
            var keep = new List<Guid> { winnerAward.ParticipantId };
            if (runnerUp != null)
                keep.Add(runnerUp.ParticipantId);
            await ReleaseDepositsAsync(auction, keep, "Savdoda yutmadingiz", cancellationToken);

            await TellBuyerAsync(auction, winnerAward, cancellationToken);
        }

        public async Task OnCancelledAsync(Auction auction, string reason, CancellationToken cancellationToken = default)
        {
            var everyone = await participants.GetAllAsync(p => p.AuctionId == auction.Id
                && (p.Status == ParticipantStatus.PENDING || p.Status == ParticipantStatus.APPROVED), cancellationToken);
            foreach (var participant in everyone)
            {
                await refunds.RequestAsync(participant, auction, "Auksion bekor qilindi", cancellationToken);
                await notifications.NotifyAsync(participant.UserId, NotificationType.AUCTION_CANCELLED,
                    new Dictionary<string, string> { ["lot"] = auction.LotNumber, ["reason"] = reason }, auction.Id, cancellationToken: cancellationToken);
            }
        }

        // ---------------- money in

        public async Task OnDepositPaidAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            var participant = payment.ParticipantId is { } participantId
                ? await participants.GetAsync(p => p.Id == participantId, cancellationToken)
                : null;
            var auction = await auctions.GetAsync(a => a.Id == payment.AuctionId, cancellationToken);
            if (participant == null || auction == null)
                return;

            var usable = participant.Status == ParticipantStatus.APPROVED
                         && participant.DepositStatus == DepositStatus.NOT_PAID
                         && auction.Status is AuctionStatus.SCHEDULED or AuctionStatus.ONGOING;
            if (!usable)
            {
                // Paid twice, or after the application was rejected or the auction cancelled: the money goes back.
                await QueueExtraRefundAsync(payment, "Zakalat ishlatib bo'lmaydi", cancellationToken);
                return;
            }

            participant.DepositStatus = DepositStatus.PAID;
            await participants.UpdateAsync(participant, cancellationToken);
            await notifications.NotifyAsync(participant.UserId, NotificationType.DEPOSIT_RECEIVED,
                new Dictionary<string, string> { ["lot"] = auction.LotNumber, ["amount"] = NotificationTemplates.Money(payment.Amount) },
                auction.Id, cancellationToken: cancellationToken);
        }

        public async Task OnFinalPaymentPaidAsync(Payment payment, DateTime now, CancellationToken cancellationToken = default)
        {
            var award = await FindLiveAwardAsync(payment.ParticipantId, payment.AuctionId, AwardStatus.ACCEPTED, cancellationToken);
            if (award?.AuctionResult == null)
            {
                logger.LogWarning("Final payment {PaymentId} arrived without an award waiting for payment; it is returned", payment.Id);
                await QueueExtraRefundAsync(payment, "Kechikib kelgan to'lov", cancellationToken);
                return;
            }

            var auction = await auctions.GetAsync(a => a.Id == payment.AuctionId, cancellationToken) ?? throw new NotFoundException("Auction not found");
            var result = award.AuctionResult;
            award.AuctionResult = null;   // the award is saved on its own; the result is saved below

            award.Status = AwardStatus.PAID;
            award.ResolvedAt = now;
            await awards.UpdateAsync(award, cancellationToken);

            result.Outcome = AuctionOutcome.SOLD;
            await results.UpdateAsync(result, cancellationToken);

            var property = await properties.GetAsync(p => p.Id == auction.PropertyId, cancellationToken);
            if (property != null)
            {
                property.Status = PropertyStatus.SOLD;
                await properties.UpdateAsync(property, cancellationToken);
            }

            // The deposit of the buyer is part of the price; whoever's deposit was held for the runner-up's sake goes back now.
            await ReleaseDepositsAsync(auction, [award.ParticipantId], "Savdo yakunlandi", cancellationToken);
            var buyer = await participants.GetAsync(p => p.Id == award.ParticipantId, cancellationToken);
            if (buyer != null)
                await notifications.NotifyAsync(buyer.UserId, NotificationType.PAYMENT_RECEIVED,
                    new Dictionary<string, string> { ["lot"] = auction.LotNumber }, auction.Id, cancellationToken: cancellationToken);
        }

        public async Task OnRefundPaidAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            var participant = payment.ParticipantId is { } participantId
                ? await participants.GetAsync(p => p.Id == participantId, cancellationToken)
                : null;
            if (participant == null)
                return;
            var auction = await auctions.GetAsync(a => a.Id == payment.AuctionId, cancellationToken);

            // A refund of money that arrived out of turn does not touch the participant's own deposit.
            if (payment.Note?.StartsWith(ExtraPaymentNote, StringComparison.Ordinal) != true && participant.DepositStatus == DepositStatus.PAID)
            {
                participant.DepositStatus = DepositStatus.REFUNDED;
                await participants.UpdateAsync(participant, cancellationToken);
            }
            await notifications.NotifyAsync(participant.UserId, NotificationType.DEPOSIT_REFUNDED,
                new Dictionary<string, string> { ["lot"] = auction?.LotNumber ?? "-", ["amount"] = NotificationTemplates.Money(payment.Amount) },
                payment.AuctionId, cancellationToken: cancellationToken);
        }

        // ---------------- the buyer does not pay

        public async Task<bool> DefaultAsync(Guid awardId, DateTime now, CancellationToken cancellationToken = default)
        {
            var award = await awards.Query(a => a.Id == awardId, cancellationToken).Include(a => a.AuctionResult).FirstOrDefaultAsync(cancellationToken);
            if (award is not { Status: AwardStatus.ACCEPTED, AuctionResult: not null })
                return false;
            var result = award.AuctionResult;
            award.AuctionResult = null;
            var auction = await auctions.GetAsync(a => a.Id == result.AuctionId, cancellationToken) ?? throw new NotFoundException("Auction not found");
            var participant = await participants.GetAsync(p => p.Id == award.ParticipantId, cancellationToken) ?? throw new NotFoundException("Participant not found");

            award.Status = AwardStatus.DEFAULTED;
            award.ResolvedAt = now;
            await awards.UpdateAsync(award, cancellationToken);

            // The bidding was a commitment: the deposit is not given back.
            participant.DepositStatus = DepositStatus.FORFEITED;
            await participants.UpdateAsync(participant, cancellationToken);
            await notifications.NotifyAsync(participant.UserId, NotificationType.WINNER_DEFAULTED,
                new Dictionary<string, string> { ["lot"] = auction.LotNumber }, auction.Id, cancellationToken: cancellationToken);

            var next = award.Rank == 1 && auction.OfferToRunnerUp
                ? await FindRunnerUpAsync(auction.Id, award.ParticipantId, cancellationToken)
                : null;
            if (next == null)
            {
                await EndUnsoldAsync(auction, result, cancellationToken);
                return true;
            }

            // Nobody is the buyer while the offer is open.
            ClearBuyer(result);
            result.Outcome = AuctionOutcome.AWAITING_RUNNER_UP;
            await results.UpdateAsync(result, cancellationToken);
            var offer = await awards.CreateAsync(new AuctionAward
            {
                AuctionResultId = result.Id,
                Rank = 2,
                ParticipantId = next.ParticipantId,
                BidId = next.Id,
                Price = next.Amount,
                Status = AwardStatus.OFFERED,
                RespondBy = now.AddDays(RunnerUpResponseDays),
            }, cancellationToken);

            var offered = await participants.GetAsync(p => p.Id == next.ParticipantId, cancellationToken);
            if (offered != null)
                await notifications.NotifyAsync(offered.UserId, NotificationType.RUNNER_UP_OFFER, new Dictionary<string, string>
                {
                    ["lot"] = auction.LotNumber,
                    ["price"] = NotificationTemplates.Money(offer.Price),
                    ["respondBy"] = NotificationTemplates.Time(offer.RespondBy!.Value),
                }, auction.Id, cancellationToken: cancellationToken);
            return true;
        }

        public async Task RefuseAsync(Guid userId, Guid auctionId, DateTime now, CancellationToken cancellationToken = default)
        {
            var participant = await ParticipantOfAsync(userId, auctionId, cancellationToken);
            var award = await FindLiveAwardAsync(participant.Id, auctionId, AwardStatus.ACCEPTED, cancellationToken)
                        ?? throw new ConflictException("You have no purchase waiting for payment in this auction.");
            await DefaultAsync(award.Id, now, cancellationToken);
        }

        // ---------------- the runner-up

        public async Task AcceptOfferAsync(Guid userId, Guid auctionId, DateTime now, CancellationToken cancellationToken = default)
        {
            var participant = await ParticipantOfAsync(userId, auctionId, cancellationToken);
            var award = await FindOpenOfferAsync(participant.Id, auctionId, now, cancellationToken);
            var result = award.AuctionResult!;
            award.AuctionResult = null;
            var auction = await auctions.GetAsync(a => a.Id == auctionId, cancellationToken) ?? throw new NotFoundException("Auction not found");

            award.Status = AwardStatus.ACCEPTED;
            award.PaymentDueAt = now.AddDays(FinalPaymentDays);
            await awards.UpdateAsync(award, cancellationToken);

            result.Outcome = AuctionOutcome.AWAITING_PAYMENT;
            result.WinnerParticipantId = award.ParticipantId;
            result.WinningBidId = award.BidId;
            result.FinalPrice = award.Price;
            result.PaymentDueAt = award.PaymentDueAt;
            await results.UpdateAsync(result, cancellationToken);

            await TellBuyerAsync(auction, award, cancellationToken);
        }

        public async Task DeclineOfferAsync(Guid userId, Guid auctionId, DateTime now, CancellationToken cancellationToken = default)
        {
            var participant = await ParticipantOfAsync(userId, auctionId, cancellationToken);
            var award = await FindOpenOfferAsync(participant.Id, auctionId, now, cancellationToken);
            await CloseOfferAsync(award, AwardStatus.DECLINED, now, cancellationToken);
        }

        public async Task<bool> ExpireOfferAsync(Guid awardId, DateTime now, CancellationToken cancellationToken = default)
        {
            var award = await awards.Query(a => a.Id == awardId, cancellationToken).Include(a => a.AuctionResult).FirstOrDefaultAsync(cancellationToken);
            if (award is not { Status: AwardStatus.OFFERED, AuctionResult: not null } || award.RespondBy > now)
                return false;
            await CloseOfferAsync(award, AwardStatus.EXPIRED, now, cancellationToken);
            return true;
        }

        private async Task CloseOfferAsync(AuctionAward award, AwardStatus status, DateTime now, CancellationToken cancellationToken)
        {
            var result = award.AuctionResult!;
            award.AuctionResult = null;
            var auction = await auctions.GetAsync(a => a.Id == result.AuctionId, cancellationToken) ?? throw new NotFoundException("Auction not found");

            award.Status = status;
            award.ResolvedAt = now;
            await awards.UpdateAsync(award, cancellationToken);
            // They were not bound (the offer came after the bidding), so their deposit goes back with everybody else's.
            await EndUnsoldAsync(auction, result, cancellationToken);
        }

        // ---------------- reminders

        public async Task<bool> RemindPaymentAsync(Guid awardId, DateTime now, CancellationToken cancellationToken = default)
        {
            var award = await awards.Query(a => a.Id == awardId, cancellationToken).Include(a => a.AuctionResult).FirstOrDefaultAsync(cancellationToken);
            if (award is not { Status: AwardStatus.ACCEPTED, AuctionResult: not null } || award.PaymentDueAt is not { } due || due <= now)
                return false;
            var auction = await auctions.GetAsync(a => a.Id == award.AuctionResult.AuctionId, cancellationToken);
            var participant = await participants.GetAsync(p => p.Id == award.ParticipantId, cancellationToken);
            if (auction == null || participant == null)
                return false;

            return await notifications.NotifyAsync(participant.UserId, NotificationType.PAYMENT_DUE_SOON, new Dictionary<string, string>
            {
                ["lot"] = auction.LotNumber,
                ["deadline"] = NotificationTemplates.Time(due),
                ["due"] = NotificationTemplates.Money(AmountDue(award, auction, participant)),
            }, auction.Id, once: true, cancellationToken) > 0;
        }

        // ---------------- helpers

        /// <summary>The price less the deposit, which counts towards it.</summary>
        public static decimal AmountDue(AuctionAward award, Auction auction, AuctionParticipant participant)
            => Math.Max(0, award.Price - (participant.DepositStatus == DepositStatus.PAID ? auction.DepositAmount : 0));

        private async Task TellBuyerAsync(Auction auction, AuctionAward award, CancellationToken cancellationToken)
        {
            var participant = await participants.GetAsync(p => p.Id == award.ParticipantId, cancellationToken);
            if (participant == null)
                return;
            await notifications.NotifyAsync(participant.UserId, NotificationType.AUCTION_WON, new Dictionary<string, string>
            {
                ["lot"] = auction.LotNumber,
                ["price"] = NotificationTemplates.Money(award.Price),
                ["due"] = NotificationTemplates.Money(AmountDue(award, auction, participant)),
                ["deadline"] = NotificationTemplates.Time(award.PaymentDueAt ?? DateTime.UtcNow),
            }, auction.Id, cancellationToken: cancellationToken);
        }

        /// <summary>The sale ends without a buyer: the property is back in the catalog and every held deposit goes back.</summary>
        private async Task EndUnsoldAsync(Auction auction, AuctionResult result, CancellationToken cancellationToken)
        {
            ClearBuyer(result);
            result.Outcome = AuctionOutcome.NOT_SOLD;
            await results.UpdateAsync(result, cancellationToken);

            var property = await properties.GetAsync(p => p.Id == auction.PropertyId, cancellationToken);
            if (property is { Status: PropertyStatus.IN_AUCTION })
            {
                property.Status = PropertyStatus.PUBLISHED;
                await properties.UpdateAsync(property, cancellationToken);
            }
            await ReleaseDepositsAsync(auction, [], "Savdo o'tmadi", cancellationToken);
        }

        private static void ClearBuyer(AuctionResult result)
        {
            result.WinnerParticipantId = null;
            result.WinningBidId = null;
            result.FinalPrice = null;
            result.PaymentDueAt = null;
        }

        /// <summary>Queues the refund of every paid deposit of the auction except those of the participants in <paramref name="keep"/>.</summary>
        private async Task ReleaseDepositsAsync(Auction auction, IReadOnlyCollection<Guid> keep, string reason, CancellationToken cancellationToken)
        {
            var paid = await participants.GetAllAsync(p => p.AuctionId == auction.Id && p.DepositStatus == DepositStatus.PAID && !keep.Contains(p.Id), cancellationToken);
            foreach (var participant in paid)
                await refunds.RequestAsync(participant, auction, reason, cancellationToken);
        }

        /// <summary>
        /// The highest bid of anybody else who may still take the property: approved, deposit paid. That bid is also its price
        /// (a runner-up pays what they themselves offered).
        /// </summary>
        private async Task<Bid?> FindRunnerUpAsync(Guid auctionId, Guid exceptParticipantId, CancellationToken cancellationToken)
            => await bids.Query(b => b.AuctionId == auctionId && b.ParticipantId != exceptParticipantId
                                     && b.Participant!.Status == ParticipantStatus.APPROVED
                                     && b.Participant.DepositStatus == DepositStatus.PAID, cancellationToken)
                         .OrderByDescending(b => b.Amount).FirstOrDefaultAsync(cancellationToken);

        private async Task<AuctionParticipant> ParticipantOfAsync(Guid userId, Guid auctionId, CancellationToken cancellationToken)
            => await participants.GetAsync(p => p.AuctionId == auctionId && p.UserId == userId && p.Status == ParticipantStatus.APPROVED, cancellationToken)
               ?? throw new NotFoundException("You are not a participant of this auction.");

        private async Task<AuctionAward?> FindLiveAwardAsync(Guid? participantId, Guid auctionId, AwardStatus status, CancellationToken cancellationToken)
            => participantId == null ? null
                : await awards.Query(a => a.ParticipantId == participantId && a.Status == status && a.AuctionResult!.AuctionId == auctionId, cancellationToken)
                              .Include(a => a.AuctionResult).FirstOrDefaultAsync(cancellationToken);

        private async Task<AuctionAward> FindOpenOfferAsync(Guid participantId, Guid auctionId, DateTime now, CancellationToken cancellationToken)
        {
            var award = await FindLiveAwardAsync(participantId, auctionId, AwardStatus.OFFERED, cancellationToken)
                        ?? throw new ConflictException("You have no offer to buy the property in this auction.");
            if (award.RespondBy <= now)
                throw new ConflictException("The offer has expired.");
            return award;
        }

        /// <summary>Money that arrived and cannot be used is returned in full, through a refund the staff carry out.</summary>
        private async Task QueueExtraRefundAsync(Payment payment, string reason, CancellationToken cancellationToken)
        {
            var note = ExtraPaymentNote + payment.Id;
            if (payment.ParticipantId == null || await payments.AnyAsync(p => p.Note == note, cancellationToken))
                return;
            await payments.CreateAsync(new Payment
            {
                UserId = payment.UserId,
                AuctionId = payment.AuctionId,
                ParticipantId = payment.ParticipantId,
                Purpose = PaymentPurpose.REFUND,
                Amount = payment.Amount,
                Status = PaymentStatus.PENDING,
                Provider = payment.Provider,
                Note = note,
            }, cancellationToken);
            logger.LogWarning("Payment {PaymentId} cannot be used ({Reason}); a refund was queued", payment.Id, reason);
        }
    }
}
