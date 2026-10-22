using Application.Models.Requests.Abstractions;
using Application.Exceptions;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices.Abstractions;
using Application.Interfaces.Repositories.Abstractions;
using Application.Models.Dtos.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Abstractions;
using MapsterMapper;
using Domain.Abstractions;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.EntityServices.Abstractions
{
    public class GenericEntityService<TEntity, TCreateDto, TUpdateDto, TEntityViewModel>(
        IGenericRepository<TEntity> repository,
        IPermissionService permissionService,
        IMapper mapper)
        : IGenericEntityService<TEntity, TCreateDto, TUpdateDto, TEntityViewModel>
        where TEntity : AuditableEntity
        where TEntityViewModel : BaseViewModel
        where TCreateDto : BaseCreateDto
        where TUpdateDto : BaseUpdateDto
    {
        protected readonly IGenericRepository<TEntity> _repository = repository;
        protected readonly IPermissionService _permissionService = permissionService;
        protected readonly IMapper _mapper = mapper;

        public virtual async Task<ResponseModel<TEntityViewModel>> CreateAsync(TCreateDto entityCreateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(TEntity).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.CREATE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            var entity = _mapper.Map<TEntity>(entityCreateDto);

            var result = await _repository.CreateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to create entity.");

            return ResponseModel<TEntityViewModel>.Ok(_mapper.Map<TEntityViewModel>(result));
        }

        public virtual async Task<ResponseModel<TEntityViewModel>> UpdateAsync(TUpdateDto entityUpdateDto, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(TEntity).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.UPDATE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            // Load the stored entity and apply only the DTO fields, so fields the DTO lacks
            // (PasswordHash, CreatedAt, CreatedBy, foreign keys) keep their values.
            var entity = await _repository.GetAsync(x => x.Id == entityUpdateDto.Id, cancellationToken)
                                ?? throw new NotFoundException("Entity not found");
            _mapper.Map(entityUpdateDto, entity);

            var result = await _repository.UpdateAsync(entity, cancellationToken)
                                ?? throw new InvalidOperationException("Failed to update entity.");

            return ResponseModel<TEntityViewModel>.Ok(_mapper.Map<TEntityViewModel>(result));
        }

        public virtual async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(TEntity).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.DELETE}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                                ?? throw new NotFoundException("Entity not found");

            return ResponseModel<bool>.Ok(await _repository.DeleteAsync(entity, cancellationToken));
        }

        public virtual async Task<ResponseModel<bool>> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(TEntity).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(predicate, cancellationToken);
            return ResponseModel<bool>.Ok(entity != null);
        }

        public virtual async Task<ResponseModel<List<TEntityViewModel>>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, int pageIndex = 0, int pageSize = BaseGetListQuery.DefaultPageSize, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(TEntity).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            // Without a stable order the database may return pages that overlap or skip rows.
            orderBy ??= q => q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id);
            var (data, totalCount) = await _repository.GetPaginatedAsync(predicate, pageIndex, pageSize, orderBy, cancellationToken);

            return ResponseModel<List<TEntityViewModel>>.Ok(_mapper.Map<List<TEntityViewModel>>(data), pageIndex, pageSize, totalCount);
        }

        public virtual async Task<ResponseModel<TEntityViewModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(TEntity).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                                ?? throw new NotFoundException("Entity not found");

            return ResponseModel<TEntityViewModel?>.Ok(_mapper.Map<TEntityViewModel>(entity));
        }

        public virtual async Task<ResponseModel<TEntityViewModel?>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            string entityTypeName = typeof(TEntity).Name.ToUpper();
            if (!await _permissionService.Check($"{OperationType.READ}_{entityTypeName}"))
                throw new ForbiddenException("You do not have permission to create this entity.");

            var entity = await _repository.GetAsync(predicate, cancellationToken)
                                ?? throw new NotFoundException("Entity not found");
            return ResponseModel<TEntityViewModel?>.Ok(_mapper.Map<TEntityViewModel>(entity));
        }
    }
}
