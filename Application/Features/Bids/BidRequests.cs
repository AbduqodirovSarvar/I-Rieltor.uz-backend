using Application.Features.Abstractions.Commands.Interfaces;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Results;
using Application.Models.ViewModels.Bids;
using Domain.Behaviours;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Bids
{
    public record PlaceBidCommand : ICommand<ResponseModel<PlacedBidViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }

        /// <summary>The bid, so'm: at least the starting price for the first bid, then the current price plus the step.</summary>
        [Range(0.01, 1_000_000_000_000_000.0)]
        public decimal Amount { get; init; }
    }

    public record GetBidListQuery : IQuery<ResponseModel<List<PublicBidViewModel>>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }

        [Range(0, 100_000)]
        public int PageIndex { get; init; }

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;
    }

    public class BidRequestHandlers(IBidService service)
        : ICommandHandler<PlaceBidCommand, ResponseModel<PlacedBidViewModel>>,
          IQueryHandler<GetBidListQuery, ResponseModel<List<PublicBidViewModel>>>
    {
        public Task<ResponseModel<PlacedBidViewModel>> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
            => service.PlaceBidAsync(request.AuctionId, request.Amount, cancellationToken);

        public Task<ResponseModel<List<PublicBidViewModel>>> Handle(GetBidListQuery request, CancellationToken cancellationToken)
            => service.GetPublicListAsync(request.AuctionId, request.PageIndex, request.PageSize, cancellationToken);
    }
}
