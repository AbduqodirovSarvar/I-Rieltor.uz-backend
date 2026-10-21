using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.Properties;
using Application.Models.Requests.Properties;
using Application.Models.Results;
using Application.Models.ViewModels.Properties;
using Application.Models.ViewModels.Public;
using Domain.Entities;

namespace Application.Interfaces.EntityServices
{
    public interface IPropertyService
        : IGenericEntityService<Property,
                                CreatePropertyDto,
                                UpdatePropertyDto,
                                PropertyViewModel>
    {
        Task<ResponseModel<PropertyMediaViewModel>> AddMediaAsync(AddPropertyMediaDto dto, CancellationToken cancellationToken = default);
        Task<ResponseModel<PropertyMediaViewModel>> UpdateMediaAsync(UpdatePropertyMediaDto dto, CancellationToken cancellationToken = default);
        Task<ResponseModel<bool>> DeleteMediaAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Public catalog: published properties and those in an auction; no permission required.
        /// </summary>
        Task<ResponseModel<List<PublicPropertyListItemViewModel>>> GetPublicListAsync(PublicPropertyFilter filter, CancellationToken cancellationToken = default);

        /// <summary>
        /// One property of the catalog with its media and auction; counts the view. Drafts and archived ones are 404.
        /// </summary>
        Task<ResponseModel<PublicPropertyDetailViewModel>> GetPublicAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
