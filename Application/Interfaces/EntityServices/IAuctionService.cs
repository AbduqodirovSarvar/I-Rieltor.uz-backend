using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.Auctions;
using Application.Models.Results;
using Application.Models.ViewModels.Auctions;
using Application.Models.ViewModels.Public;
using Domain.Entities;

namespace Application.Interfaces.EntityServices
{
    public interface IAuctionService
        : IGenericEntityService<Auction,
                                CreateAuctionDto,
                                UpdateAuctionDto,
                                AuctionViewModel>
    {
        /// <summary>DRAFT → SCHEDULED; the property goes to IN_AUCTION.</summary>
        Task<ResponseModel<AuctionViewModel>> PublishAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>SCHEDULED or ONGOING → CANCELLED with a reason; the property returns to the catalog.</summary>
        Task<ResponseModel<AuctionViewModel>> CancelAsync(CancelAuctionDto dto, CancellationToken cancellationToken = default);

        /// <summary>Auction page for visitors; no permission required, a draft is 404.</summary>
        Task<ResponseModel<PublicAuctionDetailViewModel>> GetPublicAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
