using Application.Interfaces.Cachings;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistance.Data;
using Persistance.Services.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Repositories
{
    public class UserRepository(AppDbContext context, IRedisService redisService, IUnitOfWork unitOfWork)
        : GenericRepository<User>(context, redisService, unitOfWork), IUserRepository
    {
        // Users carry PasswordHash, so they are never written to Redis.
        protected override bool UseCache => false;

        public async Task<bool> ExistsWithAnyAsync(
            string? login,
            string? email,
            string? innpinfl,
            string? phone,
            Guid? exceptUserId = null,
            CancellationToken cancellationToken = default)
        {
            login = string.IsNullOrWhiteSpace(login) ? null : login;
            // Stored trimmed (User.Email); the citext column compares without letter case.
            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            innpinfl = string.IsNullOrWhiteSpace(innpinfl) ? null : innpinfl;
            phone = string.IsNullOrWhiteSpace(phone) ? null : phone;

            if (login == null && email == null && innpinfl == null && phone == null)
                return false;

            return await _dbSet.AsNoTracking().AnyAsync(x =>
                (exceptUserId == null || x.Id != exceptUserId) &&
                ((login != null && x.Login == login) ||
                 (email != null && x.Email == email) ||
                 (innpinfl != null && x.INNPINFL == innpinfl) ||
                 (phone != null && x.Phone == phone)),
                cancellationToken);
        }
    }
}
