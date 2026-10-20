using Application.Interfaces.Repositories.Abstractions;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IUserRepository
        : IGenericRepository<User>
    {
        Task<bool> CheckLogin(string login, CancellationToken cancellationToken = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether another user already uses any of the given unique values.
        /// Empty values are skipped, so a missing login does not match every user without a login.
        /// </summary>
        Task<bool> ExistsWithAnyAsync(
            string? login,
            string? email,
            string? innpinfl,
            string? phone,
            Guid? exceptUserId = null,
            CancellationToken cancellationToken = default);
    }
}
