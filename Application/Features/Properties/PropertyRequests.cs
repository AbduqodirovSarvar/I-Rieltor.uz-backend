using Application.Extensions;
using Application.Features.Abstractions.Commands.Interfaces;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Dtos.Properties;
using Application.Models.Requests.Abstractions;
using Application.Models.Requests.Properties;
using Application.Models.Results;
using Application.Models.ViewModels.Properties;
using Application.Models.ViewModels.Public;
using Domain.Entities;

namespace Application.Features.Properties
{
    public record CreatePropertyCommand : CreatePropertyDto, ICommand<ResponseModel<PropertyViewModel>>;
    public record UpdatePropertyCommand : UpdatePropertyDto, ICommand<ResponseModel<PropertyViewModel>>;
    public record DeletePropertyCommand : DeletePropertyDto, ICommand<ResponseModel<bool>>;
    public record AddPropertyMediaCommand : AddPropertyMediaDto, ICommand<ResponseModel<PropertyMediaViewModel>>;
    public record UpdatePropertyMediaCommand : UpdatePropertyMediaDto, ICommand<ResponseModel<PropertyMediaViewModel>>;
    public record DeletePropertyMediaCommand : DeletePropertyMediaDto, ICommand<ResponseModel<bool>>;
    public record GetOnePropertyQuery : BaseGetOneQuery, IQuery<ResponseModel<PropertyViewModel?>>;
    public record GetPropertyListQuery : BaseGetListQuery, IQuery<ResponseModel<List<PropertyViewModel>>>;
    public record GetPublicPropertyListQuery : PublicPropertyFilter, IQuery<ResponseModel<List<PublicPropertyListItemViewModel>>>;
    public record GetPublicPropertyQuery : BaseGetOneQuery, IQuery<ResponseModel<PublicPropertyDetailViewModel>>;

    public class PropertyRequestHandlers(IPropertyService service)
        : ICommandHandler<CreatePropertyCommand, ResponseModel<PropertyViewModel>>,
          ICommandHandler<UpdatePropertyCommand, ResponseModel<PropertyViewModel>>,
          ICommandHandler<DeletePropertyCommand, ResponseModel<bool>>,
          ICommandHandler<AddPropertyMediaCommand, ResponseModel<PropertyMediaViewModel>>,
          ICommandHandler<UpdatePropertyMediaCommand, ResponseModel<PropertyMediaViewModel>>,
          ICommandHandler<DeletePropertyMediaCommand, ResponseModel<bool>>,
          IQueryHandler<GetOnePropertyQuery, ResponseModel<PropertyViewModel?>>,
          IQueryHandler<GetPropertyListQuery, ResponseModel<List<PropertyViewModel>>>,
          IQueryHandler<GetPublicPropertyListQuery, ResponseModel<List<PublicPropertyListItemViewModel>>>,
          IQueryHandler<GetPublicPropertyQuery, ResponseModel<PublicPropertyDetailViewModel>>
    {
        public Task<ResponseModel<PropertyViewModel>> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
            => service.CreateAsync(request, cancellationToken);

        public Task<ResponseModel<PropertyViewModel>> Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
            => service.UpdateAsync(request, cancellationToken);

        public Task<ResponseModel<bool>> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
            => service.DeleteAsync(request.Id, cancellationToken);

        public Task<ResponseModel<PropertyMediaViewModel>> Handle(AddPropertyMediaCommand request, CancellationToken cancellationToken)
            => service.AddMediaAsync(request, cancellationToken);

        public Task<ResponseModel<PropertyMediaViewModel>> Handle(UpdatePropertyMediaCommand request, CancellationToken cancellationToken)
            => service.UpdateMediaAsync(request, cancellationToken);

        public Task<ResponseModel<bool>> Handle(DeletePropertyMediaCommand request, CancellationToken cancellationToken)
            => service.DeleteMediaAsync(request.Id, cancellationToken);

        public Task<ResponseModel<PropertyViewModel?>> Handle(GetOnePropertyQuery request, CancellationToken cancellationToken)
            => service.GetByIdAsync(request.Id, cancellationToken);

        public Task<ResponseModel<List<PropertyViewModel>>> Handle(GetPropertyListQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(request.BuildPredicate<Property>(), request.PageIndex, request.PageSize, request.BuildOrderBy<Property>(), cancellationToken);

        public Task<ResponseModel<List<PublicPropertyListItemViewModel>>> Handle(GetPublicPropertyListQuery request, CancellationToken cancellationToken)
            => service.GetPublicListAsync(request, cancellationToken);

        public Task<ResponseModel<PublicPropertyDetailViewModel>> Handle(GetPublicPropertyQuery request, CancellationToken cancellationToken)
            => service.GetPublicAsync(request.Id, cancellationToken);
    }
}
