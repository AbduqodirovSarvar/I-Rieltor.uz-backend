using Application.Features.Auctions;
using Application.Messaging;
using Domain.Enums.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Auctions;
using Application.Models.ViewModels.Public;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionController(IMediator mediator)
        : BaseEntityCrudController<CreateAuctionCommand,
                                   UpdateAuctionCommand,
                                   DeleteAuctionCommand,
                                   GetOneAuctionQuery,
                                   GetAuctionListQuery>(mediator)
    {
        /// <summary>Creates a DRAFT auction. Times in UTC ("Z") or with an offset.</summary>
        [Authorize(Policy = nameof(AuctionActions.CREATE_AUCTION))]
        [HttpPost]
        [ProducesResponseType<ResponseModel<AuctionViewModel>>(200)]
        public override Task<IActionResult> Post([FromForm] CreateAuctionCommand command) => base.Post(command);

        /// <summary>Only a DRAFT auction can be changed (409 otherwise).</summary>
        [Authorize(Policy = nameof(AuctionActions.UPDATE_AUCTION))]
        [HttpPut]
        [ProducesResponseType<ResponseModel<AuctionViewModel>>(200)]
        public override Task<IActionResult> Put([FromForm] UpdateAuctionCommand command) => base.Put(command);

        /// <summary>Only a DRAFT auction can be deleted (409 otherwise): cancel the others.</summary>
        [Authorize(Policy = nameof(AuctionActions.DELETE_AUCTION))]
        [HttpDelete]
        [ProducesResponseType<ResponseModel<bool>>(200)]
        public override Task<IActionResult> Delete(DeleteAuctionCommand command) => base.Delete(command);

        /// <summary>With the result and the purchase offers once the sale is over.</summary>
        [Authorize(Policy = nameof(AuctionActions.READ_AUCTION))]
        [HttpGet]
        [ProducesResponseType<ResponseModel<AuctionViewModel>>(200)]
        public override Task<IActionResult> Get([FromQuery] GetOneAuctionQuery request) => base.Get(request);

        [Authorize(Policy = nameof(AuctionActions.READ_AUCTION))]
        [HttpGet("list")]
        [ProducesResponseType<ResponseModel<List<AuctionViewModel>>>(200)]
        public override Task<IActionResult> GetAll([FromQuery] GetAuctionListQuery request) => base.GetAll(request);

        /// <summary>DRAFT → SCHEDULED: announces the auction; its property goes to IN_AUCTION.</summary>
        [Authorize(Policy = nameof(AuctionActions.UPDATE_AUCTION))]
        [HttpPost("publish")]
        [ProducesResponseType<ResponseModel<AuctionViewModel>>(200)]
        public async Task<IActionResult> Publish([FromBody] PublishAuctionCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>SCHEDULED or ONGOING → CANCELLED, with a reason.</summary>
        [Authorize(Policy = nameof(AuctionActions.UPDATE_AUCTION))]
        [HttpPost("cancel")]
        [ProducesResponseType<ResponseModel<AuctionViewModel>>(200)]
        public async Task<IActionResult> Cancel([FromBody] CancelAuctionCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Auction page for visitors: terms, status, prices, counts, and the outcome once it is over.</summary>
        [AllowAnonymous]
        [HttpGet("public")]
        [ProducesResponseType<ResponseModel<PublicAuctionDetailViewModel>>(200)]
        public async Task<IActionResult> GetPublic([FromQuery] GetPublicAuctionQuery request)
            => Ok(await _mediator.Send(request, HttpContext.RequestAborted));
    }
}
