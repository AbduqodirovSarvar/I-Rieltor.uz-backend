using Application.Exceptions;
using Application.Interfaces.Auctions;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Notifications;
using Application.Interfaces.Repositories;
using Application.Models.Results;
using Application.Models.ViewModels.Bids;
using Application.Notifications;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.EntityServices
{
    public class BidService(
        IAuctionRepository auctions,
        IAuctionParticipantRepository participants,
        IBidRepository bids,
        ICurrentUserService currentUser,
        INotificationService notifications,
        IAuctionEvents events,
        IUnitOfWork unitOfWork) : IBidService
    {
        public async Task<ResponseModel<PlacedBidViewModel>> PlaceBidAsync(Guid auctionId, decimal amount, CancellationToken cancellationToken = default)
        {
            try
            {
                return await unitOfWork.ExecuteAsync(() => PlaceInTransactionAsync(auctionId, amount, cancellationToken), cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Somebody's bid (or the end of the sale) changed the auction between our read and our write.
                throw new ConflictException("Another bid was placed at the same moment. Check the current price and bid again.");
            }
        }

        private async Task<ResponseModel<PlacedBidViewModel>> PlaceInTransactionAsync(Guid auctionId, decimal amount, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
            var now = DateTime.UtcNow;
            var auction = await auctions.GetAsync(a => a.Id == auctionId && a.Status != AuctionStatus.DRAFT, cancellationToken)
                          ?? throw new NotFoundException("Auction not found");
            if (auction.Status != AuctionStatus.ONGOING || now >= auction.EndsAt)
                throw new ConflictException("The sale is not open for bids.");

            var participant = await participants.GetAsync(p => p.AuctionId == auctionId && p.UserId == userId && p.Status == ParticipantStatus.APPROVED, cancellationToken)
                              ?? throw new ForbiddenException("You are not an approved participant of this auction.");
            if (participant.DepositStatus != DepositStatus.PAID)
                throw new ForbiddenException("The deposit has not been paid yet.");

            amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
            var minimum = MinNextBid(auction);
            if (amount < minimum)
                throw new BadRequestException($"The bid must be at least {NotificationTemplates.Money(minimum)} so'm.");

            Bid? previousLeader = null;
            if (auction.LeadingBidId is { } leadingId)
            {
                previousLeader = await bids.GetAsync(b => b.Id == leadingId, cancellationToken);
                if (previousLeader?.ParticipantId == participant.Id)
                    throw new ConflictException("You are already the highest bidder.");
            }

            var bid = await bids.CreateAsync(new Bid
            {
                AuctionId = auctionId,
                ParticipantId = participant.Id,
                Amount = amount,
                CreatedByIp = currentUser.IpAddress,
            }, cancellationToken);

            auction.CurrentPrice = amount;
            auction.LeadingBidId = bid.Id;
            // A bid in the last ExtensionMinutes moves the end to ExtensionMinutes from now, so the others can answer.
            var extended = auction.ExtensionMinutes > 0 && auction.EndsAt - now <= TimeSpan.FromMinutes(auction.ExtensionMinutes);
            if (extended)
                auction.EndsAt = now.AddMinutes(auction.ExtensionMinutes);
            // The row version: a bid or an end that got in first makes this save fail, and the whole bid is rolled back.
            await auctions.UpdateAsync(auction, cancellationToken);

            // The one who was leading is told by email (not SMS: this can happen many times a minute).
            if (previousLeader != null)
            {
                var outbid = await participants.GetAsync(p => p.Id == previousLeader.ParticipantId, cancellationToken);
                if (outbid != null)
                    await notifications.NotifyAsync(outbid.UserId, NotificationType.OUTBID,
                        new Dictionary<string, string> { ["lot"] = auction.LotNumber, ["price"] = NotificationTemplates.Money(amount) },
                        auction.Id, cancellationToken: cancellationToken);
            }

            var count = await bids.Query(b => b.AuctionId == auctionId, cancellationToken).CountAsync(cancellationToken);
            var placed = new BidPlacedEvent(auctionId, participant.ParticipantNumber ?? 0, amount, bid.CreatedAt == default ? now : bid.CreatedAt,
                amount, MinNextBid(auction), auction.EndsAt, extended, count);
            await unitOfWork.AfterCommitAsync(() => events.BidPlacedAsync(placed));

            return ResponseModel<PlacedBidViewModel>.Ok(new PlacedBidViewModel
            {
                BidId = bid.Id,
                AuctionId = auctionId,
                Amount = amount,
                ParticipantNumber = placed.ParticipantNumber,
                PlacedAt = placed.PlacedAt,
                CurrentPrice = amount,
                MinNextBid = placed.MinNextBid,
                EndsAt = auction.EndsAt,
                Extended = extended,
            });
        }

        /// <summary>The first bid may be the starting price itself; after that, the current price plus the step.</summary>
        public static decimal MinNextBid(Auction auction)
            => auction.CurrentPrice is { } current ? current + auction.MinBidStep : auction.StartingPrice;

        public async Task<ResponseModel<List<PublicBidViewModel>>> GetPublicListAsync(Guid auctionId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (!await auctions.AnyAsync(a => a.Id == auctionId && a.Status != AuctionStatus.DRAFT, cancellationToken))
                throw new NotFoundException("Auction not found");

            var query = bids.Query(b => b.AuctionId == auctionId, cancellationToken);
            var total = await query.CountAsync(cancellationToken);
            var page = await query.Include(b => b.Participant)
                                  .OrderByDescending(b => b.Amount)   // amounts rise with time, and are unique: a stable order
                                  .Skip(pageIndex * pageSize).Take(pageSize).ToListAsync(cancellationToken);
            return ResponseModel<List<PublicBidViewModel>>.Ok(
                page.Select(b => new PublicBidViewModel(b.Participant?.ParticipantNumber ?? 0, b.Amount, b.CreatedAt)).ToList(),
                pageIndex, pageSize, total);
        }
    }
}
