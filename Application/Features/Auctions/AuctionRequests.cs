using Application.Extentions;
using Application.Features.Abstractions.Commands.Interfaces;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Dtos.Auctions;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Auctions;
using Application.Models.ViewModels.Public;
using Domain.Behaviours;
using Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Auctions
{
    public record CreateAuctionCommand : CreateAuctionDto, ICommand<ResponseModel<AuctionViewModel>>;
    public record UpdateAuctionCommand : UpdateAuctionDto, ICommand<ResponseModel<AuctionViewModel>>;
    public record DeleteAuctionCommand : DeleteAuctionDto, ICommand<ResponseModel<bool>>;
    public record PublishAuctionCommand : ICommand<ResponseModel<AuctionViewModel>>
    {
        [Required]
        [NotEmptyGuid]
        public Guid Id { get; init; }
    }
    public record CancelAuctionCommand : CancelAuctionDto, ICommand<ResponseModel<AuctionViewModel>>;
    public record GetOneAuctionQuery : BaseGetOneQuery, IQuery<ResponseModel<AuctionViewModel?>>;
    public record GetAuctionListQuery : BaseGetListQuery, IQuery<ResponseModel<List<AuctionViewModel>>>;
    public record GetPublicAuctionQuery : BaseGetOneQuery, IQuery<ResponseModel<PublicAuctionDetailViewModel>>;

    public class AuctionRequestHandlers(IAuctionService service)
        : ICommandHandler<CreateAuctionCommand, ResponseModel<AuctionViewModel>>,
          ICommandHandler<UpdateAuctionCommand, ResponseModel<AuctionViewModel>>,
          ICommandHandler<DeleteAuctionCommand, ResponseModel<bool>>,
          ICommandHandler<PublishAuctionCommand, ResponseModel<AuctionViewModel>>,
          ICommandHandler<CancelAuctionCommand, ResponseModel<AuctionViewModel>>,
          IQueryHandler<GetOneAuctionQuery, ResponseModel<AuctionViewModel?>>,
          IQueryHandler<GetAuctionListQuery, ResponseModel<List<AuctionViewModel>>>,
          IQueryHandler<GetPublicAuctionQuery, ResponseModel<PublicAuctionDetailViewModel>>
    {
        public Task<ResponseModel<AuctionViewModel>> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
            => service.CreateAsync(request, cancellationToken);

        public Task<ResponseModel<AuctionViewModel>> Handle(UpdateAuctionCommand request, CancellationToken cancellationToken)
            => service.UpdateAsync(request, cancellationToken);

        public Task<ResponseModel<bool>> Handle(DeleteAuctionCommand request, CancellationToken cancellationToken)
            => service.DeleteAsync(request.Id, cancellationToken);

        public Task<ResponseModel<AuctionViewModel>> Handle(PublishAuctionCommand request, CancellationToken cancellationToken)
            => service.PublishAsync(request.Id, cancellationToken);

        public Task<ResponseModel<AuctionViewModel>> Handle(CancelAuctionCommand request, CancellationToken cancellationToken)
            => service.CancelAsync(request, cancellationToken);

        public Task<ResponseModel<AuctionViewModel?>> Handle(GetOneAuctionQuery request, CancellationToken cancellationToken)
            => service.GetByIdAsync(request.Id, cancellationToken);

        public Task<ResponseModel<List<AuctionViewModel>>> Handle(GetAuctionListQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(request.BuildPredicate<Auction>(), request.PageIndex, request.PageSize, request.BuildOrderBy<Auction>(), cancellationToken);

        public Task<ResponseModel<PublicAuctionDetailViewModel>> Handle(GetPublicAuctionQuery request, CancellationToken cancellationToken)
            => service.GetPublicAsync(request.Id, cancellationToken);
    }
}
