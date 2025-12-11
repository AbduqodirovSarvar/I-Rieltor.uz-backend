using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Auths.Queries.ConfirmationCode
{
    public class GetConfirmationCodeQueryHandler(IAuthService authService)
        : IQueryHandler<GetConfirmationCodeQuery, ResponseModel<bool>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<bool>> Handle(GetConfirmationCodeQuery request, CancellationToken cancellationToken)
        {
            var result = await _authService.SendEmailAsync("akjhdjsh", cancellationToken);
            return ResponseModel<bool>.Ok(result);
        }
    }
}
