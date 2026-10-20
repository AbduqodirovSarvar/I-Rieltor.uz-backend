using Application.Features.Abstractions.Queries.Interfaces;
using Application.Models.Results;
using Application.Models.ViewModels.Users;

namespace Application.Features.Auths.Queries.CurrentUser
{
    public record GetCurrentUserQuery
        : IQuery<ResponseModel<UserViewModel>>
    {
    }
}
