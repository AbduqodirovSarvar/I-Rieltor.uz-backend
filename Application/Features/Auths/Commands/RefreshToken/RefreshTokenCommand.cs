using Application.Features.Abstractions.Commands.Interfaces;
using Application.Models.Dtos.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;

namespace Application.Features.Auths.Commands.RefreshToken
{
    public record RefreshTokenCommand
        : RefreshTokenDto, ICommand<ResponseModel<TokenViewModel>>
    {
    }
}
