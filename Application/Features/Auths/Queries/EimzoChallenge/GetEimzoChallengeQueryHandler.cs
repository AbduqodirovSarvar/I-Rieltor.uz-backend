using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;

namespace Application.Features.Auths.Queries.EimzoChallenge
{
    public class GetEimzoChallengeQueryHandler(IAuthService authService)
        : IQueryHandler<GetEimzoChallengeQuery, ResponseModel<EimzoChallengeResult>>
    {
        private readonly IAuthService _authService = authService;
        public async Task<ResponseModel<EimzoChallengeResult>> Handle(GetEimzoChallengeQuery request, CancellationToken cancellationToken)
        {
            var result = await _authService.GetEimzoChallengeAsync(cancellationToken);
            return ResponseModel<EimzoChallengeResult>.Ok(result);
        }
    }
}
