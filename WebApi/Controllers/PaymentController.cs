using Application.Features.Payments;
using Application.Messaging;
using Domain.Enums.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Payments;

namespace WebApi.Controllers
{
    /// <summary>
    /// Paying the deposit and the price, and the staff's side of the money (bank transfers, refunds).
    /// The providers' own callbacks are in <see cref="PaymentCallbackController"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentController(IMediator mediator) : BaseController(mediator)
    {
        /// <summary>
        /// Starts a payment: <c>purpose</c> DEPOSIT (before bidding) or FINAL_PAYMENT (the price less the deposit, after winning) and
        /// <c>provider</c> PAYME, CLICK, BANK_TRANSFER or CARD_TRANSFER. Returns the address to send the payer to (<c>url</c>) or the bank
        /// details (<c>instructions</c>). The same request returns the same payment.
        /// </summary>
        [HttpPost("checkout")]
        [ProducesResponseType<ResponseModel<CheckoutViewModel>>(200)]
        public async Task<IActionResult> Checkout([FromBody] CheckoutCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Your payments.</summary>
        [HttpGet("my")]
        [ProducesResponseType<ResponseModel<List<PaymentViewModel>>>(200)]
        public async Task<IActionResult> GetMy([FromQuery] GetMyPaymentsQuery query)
            => Ok(await _mediator.Send(query, HttpContext.RequestAborted));

        /// <summary>
        /// Staff: payments. Refunds to carry out: <c>Filters[purpose.equals]=REFUND&amp;Filters[status.equals]=PENDING</c>;
        /// bank transfers waiting for confirmation: <c>Filters[provider.equals]=BANK_TRANSFER&amp;Filters[status.equals]=PENDING</c>.
        /// </summary>
        [Authorize(Policy = nameof(PaymentActions.READ_PAYMENT))]
        [HttpGet("list")]
        [ProducesResponseType<ResponseModel<List<PaymentViewModel>>>(200)]
        public async Task<IActionResult> GetAll([FromQuery] GetPaymentListQuery query)
            => Ok(await _mediator.Send(query, HttpContext.RequestAborted));

        /// <summary>Staff: money that arrived (bank or card transfer, cash) is entered; the amount must be exactly the one due. Attach the check with POST api/Document (PaymentId); it shows in the payment's receipts.</summary>
        [Authorize(Policy = nameof(PaymentActions.CREATE_PAYMENT))]
        [HttpPost("manual")]
        [ProducesResponseType<ResponseModel<PaymentViewModel>>(200)]
        public async Task<IActionResult> RecordManual([FromBody] RecordManualPaymentCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Staff: the bank or card transfer of a pending payment arrived (attach the check first with POST api/Document, PaymentId).</summary>
        [Authorize(Policy = nameof(PaymentActions.UPDATE_PAYMENT))]
        [HttpPost("confirm")]
        [ProducesResponseType<ResponseModel<PaymentViewModel>>(200)]
        public async Task<IActionResult> Confirm([FromBody] ConfirmPaymentCommand command)
            => Ok(await _mediator.Send(command));

        /// <summary>Staff: the money of a queued refund was sent back; the deposit becomes REFUNDED and the participant is told.</summary>
        [Authorize(Policy = nameof(PaymentActions.UPDATE_PAYMENT))]
        [HttpPost("refund/complete")]
        [ProducesResponseType<ResponseModel<PaymentViewModel>>(200)]
        public async Task<IActionResult> CompleteRefund([FromBody] CompleteRefundCommand command)
            => Ok(await _mediator.Send(command));
    }
}
