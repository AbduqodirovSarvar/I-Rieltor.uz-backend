using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Auths;
using Application.Models.Results;

namespace Application.Features.Auths.Commands.Logout
{
    public record LogoutCommand
        : RefreshTokenDto, ICommand<ResponseModel<bool>>
    {
    }
}
