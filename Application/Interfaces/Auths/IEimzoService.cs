using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Auths
{
    public interface IEimzoService
    {
        Task<EimzoVerificationResult> VerifyAsync(string signedData, CancellationToken cancellationToken);
    }
}
