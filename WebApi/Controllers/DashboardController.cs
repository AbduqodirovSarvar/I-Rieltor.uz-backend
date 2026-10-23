using Application.Features.Dashboard;
using Application.Messaging;
using Domain.Enums.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;
using Application.Interfaces.Dashboard;
using Application.Models.Results;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController(IMediator mediator) : BaseController(mediator)
    {
        /// <summary>
        /// Staff: users, properties, auctions, applications, money, sales, the next auctions and a 30-day series.
        /// The numbers may be up to 30 seconds old.
        /// </summary>
        [Authorize(Policy = nameof(DashboardActions.READ_DASHBOARD))]
        [HttpGet("summary")]
        [ProducesResponseType<ResponseModel<DashboardSummary>>(200)]
        public async Task<IActionResult> GetSummary()
            => Ok(await _mediator.Send(new GetDashboardSummaryQuery(), HttpContext.RequestAborted));
    }
}
