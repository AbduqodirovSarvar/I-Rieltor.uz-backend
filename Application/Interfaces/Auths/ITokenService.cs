using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Auths
{
    public interface ITokenService
    {
        string GenerateToken(Claim[] claims);
    }
}
