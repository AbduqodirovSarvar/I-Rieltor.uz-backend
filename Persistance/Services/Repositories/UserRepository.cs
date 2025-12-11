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
        public async Task<bool> CheckLogin(string login, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AsNoTracking().AnyAsync(x => x.Login == login, cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var result = await _redisService.GetAsync<User>(email);
            if (result != null)
                return result;

            result = await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
            if (result != null)
                await _redisService.SetAsync(email, result);

            return result;
        }

        public async Task<User?> GetByUsernameAsync(string login, CancellationToken cancellationToken = default)
        {
            var result = await _redisService.GetAsync<User>(login);
            if (result != null)
                return result;

            result = await _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Login == login, cancellationToken);
            if (result != null)
                await _redisService.SetAsync(login, result);
            return result;
        }
    }
}
