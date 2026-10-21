using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Persistance.Data;
using Persistance.Services.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Repositories
{
    public class UserRoleRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<UserRole>(context, redisService, unitOfWork), IUserRoleRepository
    {
        public async Task<bool> LockAsync(Guid roleId, bool exclusive, CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction == null)
                throw new InvalidOperationException("A role lock only lasts for a transaction; call it inside IUnitOfWork.ExecuteAsync.");

            var rows = exclusive
                ? await _context.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"UserRoles\" WHERE \"Id\" = {roleId} AND NOT \"IsDeleted\" FOR UPDATE").ToListAsync(cancellationToken)
                : await _context.Database.SqlQuery<Guid>($"SELECT \"Id\" AS \"Value\" FROM \"UserRoles\" WHERE \"Id\" = {roleId} AND NOT \"IsDeleted\" FOR SHARE").ToListAsync(cancellationToken);
            return rows.Count == 1;
        }
    }
}
