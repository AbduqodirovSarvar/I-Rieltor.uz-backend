using Application.Models.Results;
using Application.Models.ViewModels.Bids;

namespace Application.Interfaces.EntityServices
{
    public interface IBidService
    {
        /// <summary>
        /// A bid of the signed-in participant. Needs an ONGOING auction before its end, an approved participation with the
        /// deposit paid, an amount of at least the starting price (the first bid) or the current price plus the step,
        /// and that the bidder is not already leading. Two bids at the same moment: one wins, the other gets 409.
        /// </summary>
        Task<ResponseModel<PlacedBidViewModel>> PlaceBidAsync(Guid auctionId, decimal amount, CancellationToken cancellationToken = default);

        /// <summary>Public bid history, newest first.</summary>
        Task<ResponseModel<List<PublicBidViewModel>>> GetPublicListAsync(Guid auctionId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    }
}
