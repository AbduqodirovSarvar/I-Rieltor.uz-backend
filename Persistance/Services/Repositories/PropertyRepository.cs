using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using Persistance.Services.Repositories.Abstractions;

namespace Persistance.Services.Repositories
{
    public class PropertyRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<Property>(context, redisService, unitOfWork), IPropertyRepository
    {
        public Task IncrementViewCountAsync(Guid id, CancellationToken cancellationToken = default)
            => _dbSet.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), cancellationToken);
    }
}
