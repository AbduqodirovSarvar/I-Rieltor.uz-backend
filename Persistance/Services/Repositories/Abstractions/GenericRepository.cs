using Application.Models.Requests.Abstractions;
using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Abstractions;
using Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Repositories.Abstractions
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity>
        where TEntity : BaseEntity
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        protected readonly IRedisService _redisService;
        protected readonly IUnitOfWork _unitOfWork;

        public GenericRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
            _redisService = redisService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Whether entities of this type are cached in Redis by Id. Turn off for entities holding secrets.
        /// </summary>
        protected virtual bool UseCache => true;

        protected virtual TimeSpan CacheTtl => TimeSpan.FromMinutes(10);

        /// <summary>
        /// Prefixed with the entity type so different tables never share a key.
        /// </summary>
        /// <remarks>"v1": bump it when cached types change incompatibly, so old entries are simply never read.</remarks>
        protected static string CacheKey(Guid id) => $"v1:{typeof(TEntity).Name.ToLowerInvariant()}:{id}";

        /// <summary>
        /// How long a deleted row's key holds "deleted". A reader that loaded the row just before the delete committed
        /// fills the cache only if the key is absent, so it cannot bring the row back for <see cref="CacheTtl"/>.
        /// </summary>
        protected virtual TimeSpan TombstoneTtl => TimeSpan.FromMinutes(1);

        // Inside a transaction the cache is touched only after commit, so a rolled-back row never reaches it.
        // If the commit outcome is unknown the key is dropped: right whether the change was saved or not.
        private async Task CacheAfterCommitAsync(TEntity entity, bool deleted = false)
        {
            var key = CacheKey(entity.Id);
            await _unitOfWork.AfterCommitAsync(() => deleted
                ? _redisService.SetAsync<TEntity?>(key, null, TombstoneTtl)
                : _redisService.SetAsync(key, entity, CacheTtl));
            _unitOfWork.OnCommitOutcomeUnknown(() => _redisService.RemoveAsync(key));
        }

        /// <summary>
        /// Outside a transaction SaveChanges commits by itself; when it fails the outcome may be unknown too
        /// (connection lost during the implicit commit), so the key is dropped as well.
        /// </summary>
        private async Task<int> SaveAsync(TEntity entity, CancellationToken cancellationToken)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch when (UseCache && _context.Database.CurrentTransaction == null)
            {
                await _redisService.RemoveAsync(CacheKey(entity.Id));
                throw;
            }
        }

        public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            if (UseCache)
                await CacheAfterCommitAsync(entity);
            return entity;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            await SaveAsync(entity, cancellationToken);
            if (UseCache)
                await CacheAfterCommitAsync(entity);
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            var deleted = await SaveAsync(entity, cancellationToken) > 0;
            // Soft-deleted rows stay tracked as Unchanged; deleting a related row later (a slide's image)
            // would then make EF sever the link and null or reject the foreign key.
            _context.Entry(entity).State = EntityState.Detached;
            if (UseCache)
                await CacheAfterCommitAsync(entity, deleted: true);
            return deleted;
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }

        public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!UseCache)
                return await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            var result = await _redisService.GetAsync<TEntity>(CacheKey(id));
            if (result != null)
                return result;

            result = await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            // Inside a transaction the row may be uncommitted (and could roll back), so it is not cached.
            // Otherwise "only if absent": a writer that committed meanwhile has already cached the newer row.
            if (result != null && _context.Database.CurrentTransaction == null)
                await _redisService.SetIfNotExistsAsync(CacheKey(id), result, CacheTtl);
            return result;
        }

        public virtual async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public virtual async Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            if (predicate != null)
                query = query.Where(predicate);

            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<(List<TEntity> Data, int TotalItems)> GetPaginatedAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            int pageIndex = 0,
            int pageSize = BaseGetListQuery.DefaultPageSize,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            int totalItems = await query.CountAsync(cancellationToken);

            if (orderBy != null)
                query = orderBy(query);

            var data = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (data, totalItems);
        }

        public virtual IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            if (predicate != null)
                query = query.Where(predicate);

            return query;
        }
    }
}
