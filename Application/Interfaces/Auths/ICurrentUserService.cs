using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Auths
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }

        /// <summary>
        /// IP address of the current request; behind a proxy it is correct only with forwarded headers enabled.
        /// </summary>
        string? IpAddress { get; }
        Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    }
}
