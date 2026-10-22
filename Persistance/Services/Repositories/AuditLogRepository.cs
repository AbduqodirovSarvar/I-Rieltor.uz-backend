using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistance.Data;
using Persistance.Services.Repositories.Abstractions;

namespace Persistance.Services.Repositories
{
    public class AuditLogRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<AuditLog>(context, redisService, unitOfWork), IAuditLogRepository
    {
        // Written in bulk by the audit writer, read only by the admin list.
        protected override bool UseCache => false;
    }
}
