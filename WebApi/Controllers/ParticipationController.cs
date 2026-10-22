using Application.Features.Participation;
using Application.Features.Payments;
using Application.Messaging;
using Domain.Enums.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebApi.Controllers.Abstractions;
using WebApi.Extensions;

namespace WebApi.Controllers
{
    /// <summary>
    /// Taking part in an auction: the application signed with E-IMZO, the staff's review, the participant's cabinet.
    /// Only accounts with a linked E-IMZO key (type INDIVIDUAL or LEGAL) can apply.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ParticipationController(IMediator mediator) : BaseController(mediator)
    {
        /// <summary>
        /// The text to sign for the auction, with this applicant's PINFL/INN in it. Sign it as is
        /// (e-imzo-client.js <c>create_pkcs7</c> of <c>text</c>) and send the result to <c>apply</c>.
        /// </summary>
        [HttpGet("application-text")]
        public async Task<IActionResult> GetApplicationText([FromQuery] GetApplicationTextQuery query)
            => Ok(await _mediator.Send(query, HttpContext.RequestAborted));

        /// <summary>Applies with the signed text (409 after the deadline or when already applied; 403 for a key that is not yours).</summary>
        [HttpPost("apply")]
        [EnableRateLimiting(WebApiDependencyInjection.SessionRateLimitPolicy)]
        public async Task<IActionResult> Apply([FromBody] ApplyCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Same, with the text signed on the phone (E-IMZO Mobile / ID-card).</summary>
        [HttpPost("apply-mobile")]
        [EnableRateLimiting(WebApiDependencyInjection.SessionRateLimitPolicy)]
        public async Task<IActionResult> ApplyMobile([FromBody] ApplyMobileCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Takes the application back before the sale starts; a paid deposit is queued for refund.</summary>
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawApplicationCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Your applications: status, number, deposit and, after the sale, the purchase offer with the amount due.</summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
            => Ok(await _mediator.Send(new GetMyParticipationsQuery(), HttpContext.RequestAborted));

        /// <summary>Staff: applications. Filters: <c>Filters[auctionId.equals]=...</c>, <c>Filters[status.equals]=PENDING</c>.</summary>
        [Authorize(Policy = nameof(AuctionParticipantActions.READ_AUCTIONPARTICIPANT))]
        [HttpGet("list")]
        public async Task<IActionResult> GetAll([FromQuery] GetParticipantListQuery query)
            => Ok(await _mediator.Send(query, HttpContext.RequestAborted));

        /// <summary>Approves a pending application: gives the participant number (and a zero deposit counts as paid).</summary>
        [Authorize(Policy = nameof(AuctionParticipantActions.UPDATE_AUCTIONPARTICIPANT))]
        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromBody] ApproveParticipantCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Rejects an application (pending or approved) with a reason the applicant sees; a paid deposit is queued for refund.</summary>
        [Authorize(Policy = nameof(AuctionParticipantActions.UPDATE_AUCTIONPARTICIPANT))]
        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromBody] RejectParticipantCommand command)
            => Ok(await _mediator.Send(command));

        // ---- after the sale

        /// <summary>
        /// The runner-up takes the property at their own highest bid (the winner did not pay). The payment deadline starts now;
        /// pay the price less the deposit with POST api/Payment/checkout (purpose FINAL_PAYMENT).
        /// </summary>
        [HttpPost("offer/accept")]
        public async Task<IActionResult> AcceptOffer([FromBody] AcceptOfferCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>The runner-up declines the offer; the deposit is queued for refund and the sale ends unsold.</summary>
        [HttpPost("offer/decline")]
        public async Task<IActionResult> DeclineOffer([FromBody] DeclineOfferCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>
        /// The buyer gives up before the deadline. The deposit is not returned, and the property goes to the runner-up if the
        /// auction offers it. Not reversible.
        /// </summary>
        [HttpPost("refuse")]
        public async Task<IActionResult> Refuse([FromBody] RefuseAwardCommand command)
            => Ok(await _mediator.Send(command));
    }
}
