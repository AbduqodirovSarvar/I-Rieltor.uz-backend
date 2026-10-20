using Application.Features.Auths.Commands.Login;
using Application.Features.Auths.Commands.LoginViaEimzo;
using Application.Features.Auths.Commands.Register;
using Application.Features.Auths.Commands.RegisterViaEimzo;
using Application.Features.Auths.Commands.Reset;
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
        [EnableRateLimiting(DepencyInjection.EimzoChallengeRateLimitPolicy)]
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

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
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

        [HttpGet("refresh-token")]
        public async Task<IActionResult> GetAccessToken([FromBody] string refleshToken)
        {
            if (refleshToken == null)
                return BadRequest("Reflesh token cannot be null");

            var result = await _mediator.Send(refleshToken);
            return Ok(result);
        }
    }
}
