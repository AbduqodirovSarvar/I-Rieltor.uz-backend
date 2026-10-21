using Application.Exceptions;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.PropertyCategories;
using Application.Models.Results;
using Application.Models.ViewModels.PropertyCategories;
using Application.Models.ViewModels.Public;
using Application.Services.EntityServices.Abstractions;
using Domain.Entities;
using Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.EntityServices
{
    public class PropertyCategoryService(
        IPropertyCategoryRepository repository,
        IPermissionService permissionService,
        IMapper mapper,
        IPropertyRepository propertyRepository)
        : GenericEntityService<PropertyCategory,
                               CreatePropertyCategoryDto,
                               UpdatePropertyCategoryDto,
                               PropertyCategoryViewModel>(repository, permissionService, mapper), IPropertyCategoryService
    {
        private readonly IPropertyRepository _propertyRepository = propertyRepository;

        public override async Task<ResponseModel<PropertyCategoryViewModel>> CreateAsync(CreatePropertyCategoryDto dto, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync(OperationType.CREATE);
            dto.Name = dto.Name.Trim();
            await EnsureValidAsync(dto.Name, dto.ParentCategoryId, null, cancellationToken);

            var entity = _mapper.Map<PropertyCategory>(dto);
            var result = await _repository.CreateAsync(entity, cancellationToken);
            return ResponseModel<PropertyCategoryViewModel>.Ok(_mapper.Map<PropertyCategoryViewModel>(result));
        }

        public override async Task<ResponseModel<PropertyCategoryViewModel>> UpdateAsync(UpdatePropertyCategoryDto dto, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync(OperationType.UPDATE);
            var entity = await _repository.GetAsync(x => x.Id == dto.Id, cancellationToken)
                         ?? throw new NotFoundException("Entity not found");
            dto.Name = dto.Name.Trim();
            await EnsureValidAsync(dto.Name, dto.ParentCategoryId, entity.Id, cancellationToken);

            _mapper.Map(dto, entity);
            var result = await _repository.UpdateAsync(entity, cancellationToken);
            return ResponseModel<PropertyCategoryViewModel>.Ok(_mapper.Map<PropertyCategoryViewModel>(result));
        }

        public override async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync(OperationType.DELETE);
            var entity = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                         ?? throw new NotFoundException("Entity not found");

            // Soft delete keeps the rows, so the foreign keys no longer stop this.
            if (await _repository.AnyAsync(c => c.ParentCategoryId == id, cancellationToken))
                throw new ConflictException("The category has subcategories; move or delete them first.");
            if (await _propertyRepository.AnyAsync(p => p.CategoryId == id, cancellationToken))
                throw new ConflictException("The category is used by properties; move them to another category first.");

            return ResponseModel<bool>.Ok(await _repository.DeleteAsync(entity, cancellationToken));
        }

        public async Task<ResponseModel<List<PublicPropertyCategoryViewModel>>> GetPublicListAsync(CancellationToken cancellationToken = default)
        {
            var categories = await _repository.Query(cancellationToken: cancellationToken)
                                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ThenBy(c => c.Id)
                                .ToListAsync(cancellationToken);
            return ResponseModel<List<PublicPropertyCategoryViewModel>>.Ok(_mapper.Map<List<PublicPropertyCategoryViewModel>>(categories));
        }

        public override Task<ResponseModel<List<PropertyCategoryViewModel>>> GetAllAsync(System.Linq.Expressions.Expression<Func<PropertyCategory, bool>>? predicate = null, int pageIndex = 0, int pageSize = Models.Requests.Abstractions.BaseGetListQuery.DefaultPageSize, Func<IQueryable<PropertyCategory>, IOrderedQueryable<PropertyCategory>>? orderBy = null, CancellationToken cancellationToken = default)
            // Default: display order, as in the catalog.
            => base.GetAllAsync(predicate, pageIndex, pageSize, orderBy ?? (q => q.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ThenBy(c => c.Id)), cancellationToken);

        private async Task EnsurePermissionAsync(OperationType operation)
        {
            if (!await _permissionService.Check($"{operation}_{nameof(PropertyCategory).ToUpper()}"))
                throw new ForbiddenException("You do not have permission for this operation.");
        }

        /// <summary>
        /// Unique name (letter case ignored), an existing parent, and no cycle (a category cannot sit below itself).
        /// </summary>
        private async Task EnsureValidAsync(string name, Guid? parentId, Guid? selfId, CancellationToken cancellationToken)
        {
            // Name is citext: the comparison ignores letter case.
            if (await _repository.AnyAsync(c => c.Name == name && (selfId == null || c.Id != selfId), cancellationToken))
                throw new ConflictException($"A category named '{name}' already exists.");

            var current = parentId;
            for (var depth = 0; current is { } id; depth++)
            {
                if (id == selfId)
                    throw new BadRequestException("A category cannot be placed below itself.");
                if (depth > 20)
                    throw new BadRequestException("Categories are nested too deeply.");
                var parent = await _repository.GetAsync(c => c.Id == id, cancellationToken)
                             ?? throw new BadRequestException("The parent category does not exist.");
                current = parent.ParentCategoryId;
            }
        }
    }
}
