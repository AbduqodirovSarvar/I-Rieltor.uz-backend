using Application.Features.Abstractions.Commands.Interfaces;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using Application.Models.ViewModels.Users;
using Domain.Behaviours;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Auths.Commands.EimzoLinkAndMobile
{
    /// <summary>The signed-in user links the E-IMZO key that signed the challenge to their account (P2-13).</summary>
    public record LinkEimzoCommand : ICommand<ResponseModel<UserViewModel>>
    {
        /// <summary>Base64 PKCS#7 of the challenge from GET api/auth/eimzo/challenge.</summary>
        [Required]
        public string SignedData { get; init; } = null!;
    }

    /// <summary>Same, with the key on the phone: the E-IMZO Mobile session that finished a sign-in.</summary>
    public record LinkEimzoMobileCommand : ICommand<ResponseModel<UserViewModel>>
    {
        [Required]
        [MaxLength(100)]
        public string DocumentId { get; init; } = null!;
    }

    public record StartEimzoMobileAuthCommand : ICommand<ResponseModel<EimzoMobileSession>>;
    public record StartEimzoMobileSignCommand : ICommand<ResponseModel<EimzoMobileSession>>;

    public record GetEimzoMobileStatusQuery : IQuery<ResponseModel<EimzoMobileStatusResult>>
    {
        [Required]
        [MaxLength(100)]
        public string DocumentId { get; init; } = null!;
    }

    public record SignInViaEimzoMobileCommand : ICommand<ResponseModel<TokenViewModel>>
    {
        [Required]
        [MaxLength(100)]
        public string DocumentId { get; init; } = null!;
    }

    public record SignUpViaEimzoMobileCommand : ICommand<ResponseModel<bool>>
    {
        [Required]
        [MaxLength(100)]
        public string DocumentId { get; init; } = null!;
    }

    public record ResetPasswordViaEimzoMobileCommand : ICommand<ResponseModel<bool>>
    {
        [Required]
        [MaxLength(100)]
        public string DocumentId { get; init; } = null!;
        [Required]
        [PasswordValidation]
        [MaxLength(128)]
        public string NewPassword { get; init; } = null!;
        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; init; } = null!;
    }

    public class EimzoLinkAndMobileHandlers(IAuthService authService, IEimzoService eimzoService)
        : ICommandHandler<LinkEimzoCommand, ResponseModel<UserViewModel>>,
          ICommandHandler<LinkEimzoMobileCommand, ResponseModel<UserViewModel>>,
          ICommandHandler<StartEimzoMobileAuthCommand, ResponseModel<EimzoMobileSession>>,
          ICommandHandler<StartEimzoMobileSignCommand, ResponseModel<EimzoMobileSession>>,
          IQueryHandler<GetEimzoMobileStatusQuery, ResponseModel<EimzoMobileStatusResult>>,
          ICommandHandler<SignInViaEimzoMobileCommand, ResponseModel<TokenViewModel>>,
          ICommandHandler<SignUpViaEimzoMobileCommand, ResponseModel<bool>>,
          ICommandHandler<ResetPasswordViaEimzoMobileCommand, ResponseModel<bool>>
    {
        public async Task<ResponseModel<UserViewModel>> Handle(LinkEimzoCommand request, CancellationToken cancellationToken)
            => ResponseModel<UserViewModel>.Ok(await authService.LinkEimzoAsync(request.SignedData, cancellationToken));

        public async Task<ResponseModel<UserViewModel>> Handle(LinkEimzoMobileCommand request, CancellationToken cancellationToken)
            => ResponseModel<UserViewModel>.Ok(await authService.LinkEimzoMobileAsync(request.DocumentId, cancellationToken));

        public async Task<ResponseModel<EimzoMobileSession>> Handle(StartEimzoMobileAuthCommand request, CancellationToken cancellationToken)
            => ResponseModel<EimzoMobileSession>.Ok(await eimzoService.StartMobileAuthAsync(cancellationToken));

        public async Task<ResponseModel<EimzoMobileSession>> Handle(StartEimzoMobileSignCommand request, CancellationToken cancellationToken)
            => ResponseModel<EimzoMobileSession>.Ok(await eimzoService.StartMobileSignAsync(cancellationToken));

        public async Task<ResponseModel<EimzoMobileStatusResult>> Handle(GetEimzoMobileStatusQuery request, CancellationToken cancellationToken)
            => ResponseModel<EimzoMobileStatusResult>.Ok(await eimzoService.GetMobileStatusAsync(request.DocumentId, cancellationToken));

        public async Task<ResponseModel<TokenViewModel>> Handle(SignInViaEimzoMobileCommand request, CancellationToken cancellationToken)
            => ResponseModel<TokenViewModel>.Ok(await authService.LoginViaEimzoMobileAsync(request.DocumentId, cancellationToken));

        public async Task<ResponseModel<bool>> Handle(SignUpViaEimzoMobileCommand request, CancellationToken cancellationToken)
            => ResponseModel<bool>.Ok(await authService.RegisterViaEimzoMobileAsync(request.DocumentId, cancellationToken), "Registration successful.");

        public async Task<ResponseModel<bool>> Handle(ResetPasswordViaEimzoMobileCommand request, CancellationToken cancellationToken)
            => ResponseModel<bool>.Ok(await authService.ResetViaEimzoMobileAsync(request.DocumentId, request.NewPassword, cancellationToken), "Password changed.");
    }
}
