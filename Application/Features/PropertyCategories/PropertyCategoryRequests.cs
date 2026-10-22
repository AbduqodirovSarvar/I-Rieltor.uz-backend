using Application.Extensions;
using Application.Features.Abstractions.Commands.Interfaces;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.EntityServices;
using Application.Models.Dtos.PropertyCategories;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.PropertyCategories;
using Application.Models.ViewModels.Public;
using Domain.Entities;

namespace Application.Features.PropertyCategories
{
    public record CreatePropertyCategoryCommand : CreatePropertyCategoryDto, ICommand<ResponseModel<PropertyCategoryViewModel>>;
    public record UpdatePropertyCategoryCommand : UpdatePropertyCategoryDto, ICommand<ResponseModel<PropertyCategoryViewModel>>;
    public record DeletePropertyCategoryCommand : DeletePropertyCategoryDto, ICommand<ResponseModel<bool>>;
    public record GetOnePropertyCategoryQuery : BaseGetOneQuery, IQuery<ResponseModel<PropertyCategoryViewModel?>>;
    public record GetPropertyCategoryListQuery : BaseGetListQuery, IQuery<ResponseModel<List<PropertyCategoryViewModel>>>;
    public record GetPublicPropertyCategoryListQuery : IQuery<ResponseModel<List<PublicPropertyCategoryViewModel>>>;

    public class PropertyCategoryRequestHandlers(IPropertyCategoryService service)
        : ICommandHandler<CreatePropertyCategoryCommand, ResponseModel<PropertyCategoryViewModel>>,
          ICommandHandler<UpdatePropertyCategoryCommand, ResponseModel<PropertyCategoryViewModel>>,
          ICommandHandler<DeletePropertyCategoryCommand, ResponseModel<bool>>,
          IQueryHandler<GetOnePropertyCategoryQuery, ResponseModel<PropertyCategoryViewModel?>>,
          IQueryHandler<GetPropertyCategoryListQuery, ResponseModel<List<PropertyCategoryViewModel>>>,
          IQueryHandler<GetPublicPropertyCategoryListQuery, ResponseModel<List<PublicPropertyCategoryViewModel>>>
    {
        public Task<ResponseModel<PropertyCategoryViewModel>> Handle(CreatePropertyCategoryCommand request, CancellationToken cancellationToken)
            => service.CreateAsync(request, cancellationToken);

        public Task<ResponseModel<PropertyCategoryViewModel>> Handle(UpdatePropertyCategoryCommand request, CancellationToken cancellationToken)
            => service.UpdateAsync(request, cancellationToken);

        public Task<ResponseModel<bool>> Handle(DeletePropertyCategoryCommand request, CancellationToken cancellationToken)
            => service.DeleteAsync(request.Id, cancellationToken);

        public Task<ResponseModel<PropertyCategoryViewModel?>> Handle(GetOnePropertyCategoryQuery request, CancellationToken cancellationToken)
            => service.GetByIdAsync(request.Id, cancellationToken);

        public Task<ResponseModel<List<PropertyCategoryViewModel>>> Handle(GetPropertyCategoryListQuery request, CancellationToken cancellationToken)
            => service.GetAllAsync(request.BuildPredicate<PropertyCategory>(), request.PageIndex, request.PageSize, request.BuildOrderBy<PropertyCategory>(), cancellationToken);

        public Task<ResponseModel<List<PublicPropertyCategoryViewModel>>> Handle(GetPublicPropertyCategoryListQuery request, CancellationToken cancellationToken)
            => service.GetPublicListAsync(cancellationToken);
    }
}
