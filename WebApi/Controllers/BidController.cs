using Application.Features.Bids;
using Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Controllers.Abstractions;
using WebApi.Extensions;
using Application.Models.Results;
using Application.Models.ViewModels.Bids;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BidController(IMediator mediator) : BaseController(mediator)
    {
        /// <summary>
        /// Places a bid (approved participants with a paid deposit, while the sale is ONGOING). 409 when another bid got in at
        /// the same moment or you already lead; 400 when the amount is below the minimum. Watchers get it at once on the
        /// SignalR hub <c>/hubs/auction</c> (event <c>BidPlaced</c>).
        /// </summary>
        [Authorize]
        [HttpPost]
        [EnableRateLimiting(WebApiDependencyInjection.BidRateLimitPolicy)]
        [ProducesResponseType<ResponseModel<PlacedBidViewModel>>(200)]
        public async Task<IActionResult> Place([FromBody] PlaceBidCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Public history of the auction, highest (latest) first. Bidders are participant numbers.</summary>
        [AllowAnonymous]
        [HttpGet("list")]
        [ProducesResponseType<ResponseModel<List<PublicBidViewModel>>>(200)]
        public async Task<IActionResult> GetList([FromQuery] GetBidListQuery query)
            => Ok(await _mediator.Send(query, HttpContext.RequestAborted));
    }
}
