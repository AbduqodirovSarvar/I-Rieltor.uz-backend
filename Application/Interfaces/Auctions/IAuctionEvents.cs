using Domain.Enums;

namespace Application.Interfaces.Auctions
{
    /// <param name="ParticipantNumber">The bidder as the other participants know them: the number, never the name.</param>
    /// <param name="MinNextBid">What the next bid must be at least.</param>
    /// <param name="EndsAt">The end of the sale after this bid (later than before when the bid extended it).</param>
    public sealed record BidPlacedEvent(
        Guid AuctionId,
        int ParticipantNumber,
        decimal Amount,
        DateTime PlacedAt,
        decimal CurrentPrice,
        decimal MinNextBid,
        DateTime EndsAt,
        bool Extended,
        int BidCount);

    public sealed record AuctionStatusChangedEvent(Guid AuctionId, AuctionStatus Status, DateTime EndsAt);

    /// <summary>
    /// What people watching an auction are told as it happens. The real implementation pushes it through SignalR to the
    /// group of the auction; sending is best-effort (a failure is logged, never fails the bid or the transition).
    /// </summary>
    public interface IAuctionEvents
    {
        Task BidPlacedAsync(BidPlacedEvent bidPlaced);
        Task StatusChangedAsync(AuctionStatusChangedEvent statusChanged);
    }

    /// <summary>Used where nobody can be watching (other hosts, unit tests).</summary>
    public sealed class NullAuctionEvents : IAuctionEvents
    {
        public Task BidPlacedAsync(BidPlacedEvent bidPlaced) => Task.CompletedTask;
        public Task StatusChangedAsync(AuctionStatusChangedEvent statusChanged) => Task.CompletedTask;
    }
}
