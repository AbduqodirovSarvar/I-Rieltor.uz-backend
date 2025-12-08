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
        Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    }
}
