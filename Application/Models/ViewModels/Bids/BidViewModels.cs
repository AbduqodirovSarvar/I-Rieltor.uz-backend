namespace Application.Models.ViewModels.Bids
{
    /// <summary>One bid in the public history: the participant number, not the person.</summary>
    public record PublicBidViewModel(int ParticipantNumber, decimal Amount, DateTime PlacedAt);

    /// <summary>What the bidder gets back right after bidding.</summary>
    public record PlacedBidViewModel
    {
        public Guid BidId { get; init; }
        public Guid AuctionId { get; init; }
        public decimal Amount { get; init; }
        public int ParticipantNumber { get; init; }
        public DateTime PlacedAt { get; init; }
        /// <summary>The price is now this (the bid just placed).</summary>
        public decimal CurrentPrice { get; init; }
        public decimal MinNextBid { get; init; }
        public DateTime EndsAt { get; init; }
        /// <summary>The bid came late enough to extend the sale.</summary>
        public bool Extended { get; init; }
    }
}
