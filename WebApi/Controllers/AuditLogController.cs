using Application.Features.AuditLogs;
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
    public class AuditLogController(IMediator mediator) : BaseController(mediator)
    {
        /// <summary>
        /// Who did what and when: state-changing requests and downloads of personal documents, with the final status.
        /// </summary>
        [Authorize(Policy = nameof(AuditLogActions.READ_AUDITLOG))]
        [HttpGet("list")]
        public async Task<IActionResult> GetAll([FromQuery] GetAuditLogListQuery request)
            => Ok(await _mediator.Send(request, HttpContext.RequestAborted));
    }
}
