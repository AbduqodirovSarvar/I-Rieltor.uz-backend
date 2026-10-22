using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistence.Data;
using Persistence.Services.Repositories.Abstractions;

namespace Persistence.Services.Repositories
{
    public class AuditLogRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<AuditLog>(context, redisService, unitOfWork), IAuditLogRepository
    {
        // Written in bulk by the audit writer, read only by the admin list.
        protected override bool UseCache => false;
    }
}
