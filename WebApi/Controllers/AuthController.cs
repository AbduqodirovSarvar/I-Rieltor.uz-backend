using Application.Features.Auths.Commands.ChangePassword;
using Application.Features.Auths.Commands.EimzoLinkAndMobile;
using Application.Features.Auths.Commands.Login;
using Application.Features.Auths.Commands.LoginViaEimzo;
using Application.Features.Auths.Commands.Logout;
using Application.Features.Auths.Commands.RefreshToken;
using Application.Features.Auths.Queries.CurrentUser;
using Application.Features.Auths.Commands.Register;
using Application.Features.Auths.Commands.RegisterViaEimzo;
using Application.Features.Auths.Commands.EmailConfirmation;
using Application.Features.Auths.Commands.ResetViaEimzo;
using Application.Features.Auths.Commands.UpdateProfile;
using Application.Features.Auths.Queries.EimzoChallenge;
using Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;
using WebApi.Controllers.Abstractions;
using WebApi.Extensions;
using Application.Models.Results;
using Application.Models.ViewModels.Auths;
using Application.Models.ViewModels.Users;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting(WebApiDependencyInjection.AuthRateLimitPolicy)]
    public class AuthController(IMediator mediator) : BaseController(mediator)
    {
        [HttpPost("sign-in")]
        [ProducesResponseType<ResponseModel<TokenViewModel>>(200)]
        public async Task<IActionResult> SignIn([FromBody] SignInCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// One-time challenge the user signs with E-IMZO before sign-in, sign-up or password reset via E-IMZO.
        /// </summary>
        [HttpGet("eimzo/challenge")]
        [EnableRateLimiting(WebApiDependencyInjection.SessionRateLimitPolicy)]
        [ProducesResponseType<ResponseModel<EimzoChallengeResult>>(200)]
        public async Task<IActionResult> GetEimzoChallenge()
        {
            // A challenge is single-use; proxies and browsers must not cache it.
            Response.Headers.CacheControl = "no-store";

            var result = await _mediator.Send(new GetEimzoChallengeQuery(), HttpContext.RequestAborted);
            return Ok(result);
        }

        [HttpPost("sign-in-via-eimzo")]
        [ProducesResponseType<ResponseModel<TokenViewModel>>(200)]
        public async Task<IActionResult> SignInViaEimzo([FromBody] SignInViaEimzoCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("sign-up")]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> SignUp([FromBody] SignUpCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("sign-up-via-eimzo")]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> SignUpViaEimzo([FromBody] SignUpViaEimzoCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("reset-password-via-eimzo")]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> ResetPasswordViaEimzo([FromBody] ResetPasswordViaEimzoCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        // ---- E-IMZO Mobile / ID-card: the key is on the phone. Start a session, show the QR code or deeplink
        // (built by the frontend with e-imzo-mobile.js from siteId, documentId and the hash of the challenge or document),
        // poll the status, then finish with the documentId.

        /// <summary>
        /// Starts a sign-in session: <c>{ siteId, documentId, challenge }</c>. The QR code is siteId + documentId + GOST hash of
        /// the challenge + CRC32 (see e-imzo-mobile.js).
        /// </summary>
        [HttpPost("eimzo/mobile/auth/start")]
        [EnableRateLimiting(WebApiDependencyInjection.SessionRateLimitPolicy)]
        [ProducesResponseType<ResponseModel<EimzoMobileSession>>(200)]
        public async Task<IActionResult> StartEimzoMobileAuth()
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(await _mediator.Send(new StartEimzoMobileAuthCommand(), HttpContext.RequestAborted));
        }

        /// <summary>
        /// Starts a session for signing a document on the phone (for example the auction application).
        /// </summary>
        [HttpPost("eimzo/mobile/sign/start")]
        [Authorize]
        [EnableRateLimiting(WebApiDependencyInjection.SessionRateLimitPolicy)]
        [ProducesResponseType<ResponseModel<EimzoMobileSession>>(200)]
        public async Task<IActionResult> StartEimzoMobileSign()
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(await _mediator.Send(new StartEimzoMobileSignCommand(), HttpContext.RequestAborted));
        }

        /// <summary>
        /// Poll every 1–2 seconds: status 2 = waiting for the phone, 1 = signed (finish with the documentId), -2 = expired.
        /// </summary>
        [HttpGet("eimzo/mobile/status")]
        [DisableRateLimiting]
        [ProducesResponseType<ResponseModel<EimzoMobileStatusResult>>(200)]
        public async Task<IActionResult> GetEimzoMobileStatus([FromQuery] GetEimzoMobileStatusQuery query)
        {
            Response.Headers.CacheControl = "no-store";
            return Ok(await _mediator.Send(query, HttpContext.RequestAborted));
        }

        [HttpPost("sign-in-via-eimzo-mobile")]
        [ProducesResponseType<ResponseModel<TokenViewModel>>(200)]
        public async Task<IActionResult> SignInViaEimzoMobile([FromBody] SignInViaEimzoMobileCommand command)
            => Ok(await _mediator.Send(command));

        [HttpPost("sign-up-via-eimzo-mobile")]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> SignUpViaEimzoMobile([FromBody] SignUpViaEimzoMobileCommand command)
            => Ok(await _mediator.Send(command));

        [HttpPost("reset-password-via-eimzo-mobile")]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> ResetPasswordViaEimzoMobile([FromBody] ResetPasswordViaEimzoMobileCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>
        /// Links the E-IMZO key that signed the challenge (GET api/auth/eimzo/challenge) to the signed-in account:
        /// PINFL or INN, account type and organisation name come from the certificate. After that the account can
        /// take part in auctions. One key per account and one account per key.
        /// </summary>
        [HttpPost("me/eimzo")]
        [Authorize]
        [ProducesResponseType<ResponseModel<UserViewModel>>(200)]
        public async Task<IActionResult> LinkEimzo([FromBody] LinkEimzoCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>
        /// The same with the key on the phone: <c>documentId</c> of a finished sign-in session.
        /// </summary>
        [HttpPost("me/eimzo-mobile")]
        [Authorize]
        [ProducesResponseType<ResponseModel<UserViewModel>>(200)]
        public async Task<IActionResult> LinkEimzoMobile([FromBody] LinkEimzoMobileCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>
        /// Exchanges a refresh token for a new access token and a new refresh token; the old one stops working.
        /// </summary>
        [HttpPost("refresh-token")]
        [EnableRateLimiting(WebApiDependencyInjection.SessionRateLimitPolicy)]
        [ProducesResponseType<ResponseModel<TokenViewModel>>(200)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Ends the session of the given refresh token. The access token stays valid until it expires.
        /// </summary>
        [HttpPost("logout")]
        [EnableRateLimiting(WebApiDependencyInjection.SessionRateLimitPolicy)]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Profile of the signed-in user.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [DisableRateLimiting]
        [ProducesResponseType<ResponseModel<UserViewModel>>(200)]
        public async Task<IActionResult> Me()
        {
            var result = await _mediator.Send(new GetCurrentUserQuery(), HttpContext.RequestAborted);
            return Ok(result);
        }

        /// <summary>
        /// Profile settings: sets or changes the email and sends a confirmation link to it.
        /// </summary>
        [HttpPut("me/email")]
        [Authorize]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> ChangeMyEmail([FromBody] ChangeMyEmailCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Profile settings: the signed-in user edits their own name (not for E-IMZO-verified accounts), phones and address.
        /// </summary>
        [HttpPut("me/profile")]
        [Authorize]
        [ProducesResponseType<ResponseModel<UserViewModel>>(200)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Changes the signed-in user's password (current password required). Other sessions end; the response carries new tokens.
        /// </summary>
        [HttpPut("me/password")]
        [Authorize]
        [ProducesResponseType<ResponseModel<TokenViewModel>>(200)]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangeMyPasswordCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Sends the confirmation link again to the current, still unconfirmed email.
        /// </summary>
        [HttpPost("me/email/resend-confirmation")]
        [Authorize]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> ResendEmailConfirmation()
        {
            var result = await _mediator.Send(new ResendEmailConfirmationCommand());
            return Ok(result);
        }

        /// <summary>
        /// Called by the frontend page the emailed link opens, with the token from that link.
        /// </summary>
        [HttpPost("confirm-email")]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
