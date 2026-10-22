using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistence.Data;
using Persistence.Services.Repositories.Abstractions;

namespace Persistence.Services.Repositories
{
    public class PrivateDocumentRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<PrivateDocument>(context, redisService, unitOfWork), IPrivateDocumentRepository
    {
        // Read by the owner or staff rarely and always checked for access; not worth a cache entry that could outlive a delete.
        protected override bool UseCache => false;
    }
}
