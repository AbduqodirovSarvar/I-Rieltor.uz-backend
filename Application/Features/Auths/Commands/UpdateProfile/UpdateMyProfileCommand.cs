using Application.Features.Abstractions.Commands.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Users;
using Domain.Behaviours;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Auths.Commands.UpdateProfile
{
    /// <summary>
    /// The signed-in user edits their own contact details. A value left out (null) stays as it is; an empty text clears a
    /// phone or the address. The name of an account bound to an E-IMZO key comes from the key and cannot be changed here;
    /// the e-mail and the password have their own endpoints (they need the current password or a confirmation link).
    /// </summary>
    public record UpdateMyProfileCommand : ICommand<ResponseModel<UserViewModel>>
    {
        /// <summary>The whole name in one line ("Familiya Ism Otasining ismi"); not for E-IMZO-verified accounts.</summary>
        [MaxLength(300)]
        public string? FullName { get; init; }

        [PhoneNumberValidation]
        public string? Phone { get; init; }

        [PhoneNumberValidation]
        public string? SecondaryPhone { get; init; }

        [MaxLength(300)]
        public string? Address { get; init; }
    }

    public class UpdateMyProfileCommandHandler(IAuthService authService)
        : ICommandHandler<UpdateMyProfileCommand, ResponseModel<UserViewModel>>
    {
        private readonly IAuthService _authService = authService;

        public async Task<ResponseModel<UserViewModel>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            var result = await _authService.UpdateMyProfileAsync(request, cancellationToken);
            return ResponseModel<UserViewModel>.Ok(result, "Profile updated.");
        }
    }
}
