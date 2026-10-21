using Application.Interfaces.Auctions;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services.Auctions
{
    /// <summary>
    /// Moves auctions along by the clock: SCHEDULED → ONGOING at StartsAt, ONGOING → FINISHED or FAILED at EndsAt
    /// (EndsAt as it is by then: bidding can extend it). Safe to run in several instances at once and to run again
    /// after a pause: every step re-reads the auction inside its own transaction and does nothing when it is no
    /// longer due, and the row version (xmin) lets only one writer win; the loser waits for the next run.
    /// </summary>
    public sealed class AuctionLifecycleService(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionLifecycleService> logger) : IAuctionLifecycleService
    {
        public async Task<AuctionLifecycleRun> RunOnceAsync(DateTime now, CancellationToken cancellationToken = default)
        {
            List<Guid> starting, ending;
            using (var scope = scopeFactory.CreateScope())
            {
                var auctions = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
                starting = await auctions.Query(a => a.Status == AuctionStatus.SCHEDULED && a.StartsAt <= now, cancellationToken)
                                             .OrderBy(a => a.StartsAt).Select(a => a.Id).ToListAsync(cancellationToken);
            }

            var started = 0;
            foreach (var id in starting)
                if (await TryAsync(id, "start", (steps, token) => steps.StartAsync(id, now, token), cancellationToken))
                    started++;

            // After the starts: an auction whose whole window passed while the service was down is started and finished in one run.
            using (var scope = scopeFactory.CreateScope())
            {
                var auctions = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
                ending = await auctions.Query(a => a.Status == AuctionStatus.ONGOING && a.EndsAt <= now, cancellationToken)
                                       .OrderBy(a => a.EndsAt).Select(a => a.Id).ToListAsync(cancellationToken);
            }

            var finished = 0;
            foreach (var id in ending)
                if (await TryAsync(id, "finish", (steps, token) => steps.FinishAsync(id, now, token), cancellationToken))
                    finished++;

            return new AuctionLifecycleRun(started, finished);
        }

        /// <summary>
        /// Every step gets its own scope, so its own DbContext: what one step tracked or left half-done in a failed
        /// save cannot reach the next one.
        /// </summary>
        private async Task<bool> TryAsync(Guid auctionId, string step, Func<AuctionTransitions, CancellationToken, Task<bool>> action, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                return await action(scope.ServiceProvider.GetRequiredService<AuctionTransitions>(), cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Someone else changed the auction at the same moment (an admin cancelled it, another instance
                // finished it): their change stands, and this one is looked at again in the next run.
                logger.LogInformation(ex, "Auction {AuctionId} was changed meanwhile; its {Step} is checked again in the next run", auctionId, step);
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A real fault (database error, a rule broken by the data): reported as an error (and in the alert
                // email), and the other auctions of this run still get their turn.
                logger.LogError(ex, "Auction {AuctionId}: {Step} failed", auctionId, step);
                return false;
            }
        }
    }

    /// <summary>
    /// The two transitions themselves; used through <see cref="AuctionLifecycleService"/>, one instance per step.
    /// </summary>
    public sealed class AuctionTransitions(
        IAuctionRepository auctions,
        IAuctionParticipantRepository participants,
        IBidRepository bids,
        IAuctionResultRepository results,
        IAuctionAwardRepository awards,
        IPropertyRepository properties,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<AuctionTransitions> logger)
    {
        public Task<bool> StartAsync(Guid id, DateTime now, CancellationToken cancellationToken)
            => unitOfWork.ExecuteAsync(async () =>
            {
                var auction = await auctions.GetAsync(a => a.Id == id, cancellationToken);
                if (auction is not { Status: AuctionStatus.SCHEDULED } || auction.StartsAt > now)
                    return false;

                auction.Status = AuctionStatus.ONGOING;
                await auctions.UpdateAsync(auction, cancellationToken);
                logger.LogInformation("Auction {Lot} started", auction.LotNumber);
                return true;
            }, cancellationToken);

        /// <summary>
        /// The highest bid of an approved participant wins (bid amounts are unique within an auction, so there is no tie).
        /// No approved participant or no bid: the sale did not take place (FAILED) and the property returns to the catalog.
        /// Otherwise the winner is bound to buy: an accepted award with a payment deadline. Refunding the other
        /// participants' deposits and offering the property to the runner-up are the payment flow's job (P2-06).
        /// </summary>
        public Task<bool> FinishAsync(Guid id, DateTime now, CancellationToken cancellationToken)
            => unitOfWork.ExecuteAsync(async () =>
            {
                var auction = await auctions.GetAsync(a => a.Id == id, cancellationToken);
                if (auction is not { Status: AuctionStatus.ONGOING } || auction.EndsAt > now)
                    return false;

                var approved = await participants.Query(p => p.AuctionId == id && p.Status == ParticipantStatus.APPROVED, cancellationToken)
                                                 .CountAsync(cancellationToken);
                var winning = approved == 0 ? null
                    : await bids.Query(b => b.AuctionId == id && b.Participant!.Status == ParticipantStatus.APPROVED, cancellationToken)
                                .OrderByDescending(b => b.Amount).FirstOrDefaultAsync(cancellationToken);

                var result = new AuctionResult { AuctionId = id };
                if (winning == null)
                {
                    auction.Status = AuctionStatus.FAILED;
                    result.Outcome = approved == 0 ? AuctionOutcome.NO_PARTICIPANTS : AuctionOutcome.NO_BIDS;
                    await ReleasePropertyAsync(auction.PropertyId, cancellationToken);
                }
                else
                {
                    var paymentDays = configuration.GetValue("Auctions:FinalPaymentDays", 5);
                    var due = now.AddDays(paymentDays);
                    auction.Status = AuctionStatus.FINISHED;
                    auction.CurrentPrice = winning.Amount;
                    auction.LeadingBidId = winning.Id;
                    result.Outcome = AuctionOutcome.AWAITING_PAYMENT;
                    result.WinnerParticipantId = winning.ParticipantId;
                    result.WinningBidId = winning.Id;
                    result.FinalPrice = winning.Amount;
                    result.PaymentDueAt = due;
                }

                await auctions.UpdateAsync(auction, cancellationToken);
                await results.CreateAsync(result, cancellationToken);
                if (winning != null)
                    await awards.CreateAsync(new AuctionAward
                    {
                        AuctionResultId = result.Id,
                        Rank = 1,
                        ParticipantId = winning.ParticipantId,
                        BidId = winning.Id,
                        Price = winning.Amount,
                        // The winner won the bidding: bound to buy, not asked.
                        Status = AwardStatus.ACCEPTED,
                        PaymentDueAt = result.PaymentDueAt,
                    }, cancellationToken);

                logger.LogInformation("Auction {Lot} finished: {Outcome}", auction.LotNumber, result.Outcome);
                return true;
            }, cancellationToken);

        private async Task ReleasePropertyAsync(Guid propertyId, CancellationToken cancellationToken)
        {
            var property = await properties.GetAsync(p => p.Id == propertyId, cancellationToken);
            if (property is { Status: PropertyStatus.IN_AUCTION })
            {
                property.Status = PropertyStatus.PUBLISHED;
                await properties.UpdateAsync(property, cancellationToken);
            }
        }
    }
}
