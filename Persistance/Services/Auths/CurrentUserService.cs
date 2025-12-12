using Application.Interfaces.Auths;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Persistance.Services.Auths
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IUserRepository _userRepository;

        public CurrentUserService(
            IHttpContextAccessor contextAccessor,
            IUserRepository userRepository
            )
        {
            _userRepository = userRepository;
            var httpContext = contextAccessor.HttpContext;
            if (httpContext == null)
                return;

            var idClaim = httpContext?.User.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (idClaim != null && Guid.TryParse(idClaim.Value, out Guid value))
            {
                UserId = value;
            }
        }
        public Guid? UserId {get;}

        public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            if (UserId == null)
                return null;
            
            return await _userRepository.GetByIdAsync(UserId.Value, cancellationToken);
        }
    }
}
