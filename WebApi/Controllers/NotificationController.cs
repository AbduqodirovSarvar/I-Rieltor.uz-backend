using Application.Features.Notifications;
using Application.Messaging;
using Domain.Enums.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController(IMediator mediator) : BaseController(mediator)
    {
        /// <summary>
        /// The delivery log, newest first. Filters, e.g. <c>Filters[status.equals]=FAILED</c>.
        /// </summary>
        [Authorize(Policy = nameof(NotificationActions.READ_NOTIFICATION))]
        [HttpGet("list")]
        public async Task<IActionResult> GetAll([FromQuery] GetNotificationListQuery request)
            => Ok(await _mediator.Send(request, HttpContext.RequestAborted));
    }
}
