using Application.Interfaces.Cachings;
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

        public GenericRepository(AppDbContext context, IRedisService redisService)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
            _redisService = redisService;
        }

        /// <summary>
        /// Whether entities of this type are cached in Redis by Id. Turn off for entities holding secrets.
        /// </summary>
        protected virtual bool UseCache => true;

        protected virtual TimeSpan CacheTtl => TimeSpan.FromMinutes(10);

        /// <summary>
        /// Prefixed with the entity type so different tables never share a key.
        /// </summary>
        protected static string CacheKey(Guid id) => $"{typeof(TEntity).Name.ToLowerInvariant()}:{id}";

        public virtual async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            if (UseCache)
                await _redisService.SetAsync(CacheKey(entity.Id), entity, CacheTtl);
            return entity;
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
            if (UseCache)
                await _redisService.SetAsync(CacheKey(entity.Id), entity, CacheTtl);
            return entity;
        }

        public virtual async Task<bool> DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            _dbSet.Remove(entity);
            var deleted = await _context.SaveChangesAsync(cancellationToken) > 0;
            if (UseCache)
                await _redisService.RemoveAsync(CacheKey(entity.Id));
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
            if (result != null)
                await _redisService.SetAsync(CacheKey(id), result, CacheTtl);
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
            int pageSize = 10,
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
