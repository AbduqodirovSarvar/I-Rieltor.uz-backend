using Application.Features.Auths.Commands.ChangePassword;
using Application.Features.Auths.Commands.Login;
using Application.Features.Auths.Commands.LoginViaEimzo;
using Application.Features.Auths.Commands.Logout;
using Application.Features.Auths.Commands.RefreshToken;
using Application.Features.Auths.Queries.CurrentUser;
using Application.Features.Auths.Commands.Register;
using Application.Features.Auths.Commands.RegisterViaEimzo;
using Application.Features.Auths.Commands.EmailConfirmation;
using Application.Features.Auths.Commands.ResetViaEimzo;
using Application.Features.Auths.Queries.EimzoChallenge;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;
using WebApi.Controllers.Abstractions;
using WebApi.Extentions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting(DepencyInjection.AuthRateLimitPolicy)]
    public class AuthController(IMediator mediator) : BaseController(mediator)
    {
        [HttpPost("sign-in")]
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
        [EnableRateLimiting(DepencyInjection.SessionRateLimitPolicy)]
        public async Task<IActionResult> GetEimzoChallenge()
        {
            // A challenge is single-use; proxies and browsers must not cache it.
            Response.Headers.CacheControl = "no-store";

            var result = await _mediator.Send(new GetEimzoChallengeQuery());
            return Ok(result);
        }

        [HttpPost("sign-in-via-eimzo")]
        public async Task<IActionResult> SignInViaEimzo([FromBody] SignInViaEimzoCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("sign-up")]
        public async Task<IActionResult> SignUp([FromBody] SignUpCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("sign-up-via-eimzo")]
        public async Task<IActionResult> SignUpViaEimzo([FromBody] SignUpViaEimzoCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("reset-password-via-eimzo")]
        public async Task<IActionResult> ResetPasswordViaEimzo([FromBody] ResetPasswordViaEimzoCommand command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Exchanges a refresh token for a new access token and a new refresh token; the old one stops working.
        /// </summary>
        [HttpPost("refresh-token")]
        [EnableRateLimiting(DepencyInjection.SessionRateLimitPolicy)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Ends the session of the given refresh token. The access token stays valid until it expires.
        /// </summary>
        [HttpPost("logout")]
        [EnableRateLimiting(DepencyInjection.SessionRateLimitPolicy)]
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
        public async Task<IActionResult> Me()
        {
            var result = await _mediator.Send(new GetCurrentUserQuery());
            return Ok(result);
        }

        /// <summary>
        /// Profile settings: sets or changes the email and sends a confirmation link to it.
        /// </summary>
        [HttpPut("me/email")]
        [Authorize]
        public async Task<IActionResult> ChangeMyEmail([FromBody] ChangeMyEmailCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Changes the signed-in user's password (current password required). Other sessions end; the response carries new tokens.
        /// </summary>
        [HttpPut("me/password")]
        [Authorize]
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
        public async Task<IActionResult> ResendEmailConfirmation()
        {
            var result = await _mediator.Send(new ResendEmailConfirmationCommand());
            return Ok(result);
        }

        /// <summary>
        /// Called by the frontend page the emailed link opens, with the token from that link.
        /// </summary>
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
