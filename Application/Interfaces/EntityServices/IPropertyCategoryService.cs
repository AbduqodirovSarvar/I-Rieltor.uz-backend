using Application.Interfaces.EntityServices.Abstractions;
using Application.Models.Dtos.PropertyCategories;
using Application.Models.Results;
using Application.Models.ViewModels.PropertyCategories;
using Application.Models.ViewModels.Public;
using Domain.Entities;

namespace Application.Interfaces.EntityServices
{
    public interface IPropertyCategoryService
        : IGenericEntityService<PropertyCategory,
                                CreatePropertyCategoryDto,
                                UpdatePropertyCategoryDto,
                                PropertyCategoryViewModel>
    {
        /// <summary>
        /// Every category, in display order; no permission required (catalog filters).
        /// </summary>
        Task<ResponseModel<List<PublicPropertyCategoryViewModel>>> GetPublicListAsync(CancellationToken cancellationToken = default);
    }
}
