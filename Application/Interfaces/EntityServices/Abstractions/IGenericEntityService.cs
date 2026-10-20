using Application.Models.Dtos.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Abstractions;
using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.EntityServices.Abstractions
{
    public interface IGenericEntityService<TEntity, TCreateDto, TUpdateDto, TEntityViewModel>
        where TEntity : AudiTableEntity
        where TEntityViewModel : BaseViewModel
        where TCreateDto : BaseCreateDto
        where TUpdateDto : BaseUpdateDto
    {
        Task<ResponseModel<TEntityViewModel>> CreateAsync(TCreateDto entity, CancellationToken cancellationToken = default);
        Task<ResponseModel<TEntityViewModel>> UpdateAsync(TUpdateDto entity, CancellationToken cancellationToken = default);
        Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ResponseModel<TEntityViewModel?>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
        Task<ResponseModel<TEntityViewModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<ResponseModel<List<TEntityViewModel>>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, int pageIndex = 0, int pageSize = 20, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken cancellationToken = default);
        Task<ResponseModel<bool>> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
