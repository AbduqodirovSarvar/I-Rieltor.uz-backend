using Application.Interfaces.Repositories.Abstractions;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IUserRoleRepository
        : IGenericRepository<UserRole>
    {
        /// <summary>
        /// Row lock on the role until the current transaction ends (must run inside <c>IUnitOfWork.ExecuteAsync</c>).
        /// Exclusive: deleting the role; shared: giving it to a user. So a role is never deleted while someone
        /// is being given it, and nobody is given a role that is being deleted. False if the role does not exist.
        /// </summary>
        Task<bool> LockAsync(Guid roleId, bool exclusive, CancellationToken cancellationToken = default);
    }
}
