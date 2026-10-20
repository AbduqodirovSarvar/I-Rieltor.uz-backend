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
    public class UserRepository(AppDbContext context, IRedisService redisService)
        : GenericRepository<User>(context, redisService), IUserRepository
    {
        // Users carry PasswordHash, so they are never written to Redis.
        protected override bool UseCache => false;

        public async Task<bool> CheckLogin(string login, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().AnyAsync(x => x.Login == login, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Login == login, cancellationToken);
        }

        public async Task<bool> ExistsWithAnyAsync(
            string? login,
            string? email,
            string? innpinfl,
            string? phone,
            Guid? exceptUserId = null,
            CancellationToken cancellationToken = default)
        {
            login = string.IsNullOrWhiteSpace(login) ? null : login;
            email = string.IsNullOrWhiteSpace(email) ? null : email;
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
