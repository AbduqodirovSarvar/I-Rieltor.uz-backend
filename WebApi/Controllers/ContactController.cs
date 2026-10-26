using Application.Features.Contact;
using Application.Messaging;
using Application.Models.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Controllers.Abstractions;
using WebApi.Extensions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    [EnableRateLimiting(WebApiDependencyInjection.AuthRateLimitPolicy)]
    public class ContactController(IMediator mediator) : BaseController(mediator)
    {
        /// <summary>
        /// The contact form of the public site. E-mailed to the company; nothing is stored. A hidden field
        /// <c>website</c> must stay empty (a trap for robots).
        /// </summary>
        [HttpPost]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public async Task<IActionResult> Send([FromBody] SendContactMessageCommand command)
            => Ok(await _mediator.Send(command, HttpContext.RequestAborted));
    }
}
