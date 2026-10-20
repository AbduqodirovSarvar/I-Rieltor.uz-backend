using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Results;

namespace Application.Features.Auths.Queries.EimzoChallenge
{
    public record GetEimzoChallengeQuery
        : IQuery<ResponseModel<EimzoChallengeResult>>
    {
    }
}
