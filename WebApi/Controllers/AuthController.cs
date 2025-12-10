using Application.Models.Dtos.Auths;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IMediator mediator) : BaseController(mediator)
    {
        [HttpPost("sign-in")]
        public async Task<IActionResult> SignIn([FromBody] LoginDto command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("sign-in-via-eimzo")]
        public async Task<IActionResult> SignInViaEimzo([FromBody] LoginViaEimzoDto command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("sign-up")]
        public async Task<IActionResult> SignUp([FromBody] RegisterDto command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("sign-up-via-eimzo")]
        public async Task<IActionResult> SignUpViaEimzo([FromBody] RegisterViaEimzoDto command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto command)
        {
            if (command == null)
                return BadRequest("Request cannot be null");

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpPost("reset-password-via-eimzo")]
        public async Task<IActionResult> ResetPasswordViaEimzo([FromBody] ResetPasswordByEImzoDto command)
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
