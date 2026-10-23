using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Payments;
using Domain.Entities;
using Domain.Enums;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Application.Interfaces.Payments
{
    /// <summary>
    /// The one place where a payment becomes PAID: saves it and lets the settlement react by purpose
    /// (deposit → the participant may bid, final payment → sold, refund → deposit returned). Runs inside the caller's transaction.
    /// </summary>
    public interface IPaymentProcessor
    {
        Task MarkPaidAsync(Payment payment, DateTime now, CancellationToken cancellationToken = default);
    }

    public interface IPaymentService
    {
        /// <summary>
        /// Starts a payment of the signed-in participant: the deposit, or what is left of the price after the deposit.
        /// Asking again for the same payment returns the same one. 409 when the method is off or nothing is due.
        /// </summary>
        Task<ResponseModel<CheckoutViewModel>> CheckoutAsync(Guid auctionId, PaymentPurpose purpose, PaymentProvider provider, CancellationToken cancellationToken = default);

        /// <summary>The methods whose settings are filled in (a checkout with any other is a 409).</summary>
        List<PaymentProvider> GetAvailableProviders();

        Task<ResponseModel<List<PaymentViewModel>>> GetMyAsync(BaseGetListQuery query, CancellationToken cancellationToken = default);

        /// <summary>Staff: all payments, e.g. <c>Filters[purpose.equals]=REFUND</c> and <c>Filters[status.equals]=PENDING</c> for the refunds to carry out.</summary>
        Task<ResponseModel<List<PaymentViewModel>>> GetListAsync(BaseGetListQuery query, CancellationToken cancellationToken = default);

        /// <summary>Staff: money that arrived by bank transfer or any other way, entered by hand; it counts at once.</summary>
        Task<ResponseModel<PaymentViewModel>> RecordManualAsync(Guid participantId, PaymentPurpose purpose, decimal amount, PaymentProvider provider, string? reference, string? note, CancellationToken cancellationToken = default);

        /// <summary>Staff: the bank transfer of a pending payment has arrived.</summary>
        Task<ResponseModel<PaymentViewModel>> ConfirmAsync(Guid paymentId, string? reference, CancellationToken cancellationToken = default);

        /// <summary>Staff: a queued refund has been carried out (the money was sent back).</summary>
        Task<ResponseModel<PaymentViewModel>> CompleteRefundAsync(Guid paymentId, string? reference, CancellationToken cancellationToken = default);
    }

    /// <summary>Payme Merchant API (JSON-RPC 2.0): Payme calls us; the answer is always HTTP 200 with a result or an error.</summary>
    public interface IPaymeMerchantService
    {
        Task<JsonObject> HandleAsync(JsonElement request, string? authorizationHeader, CancellationToken cancellationToken = default);
    }

    public enum ClickStep
    {
        Prepare,
        Complete,
    }

    /// <summary>Click SHOP-API: prepare (action 0) and complete (action 1), signed with MD5 by Click.</summary>
    public interface IClickShopService
    {
        Task<JsonObject> HandleAsync(ClickStep step, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken = default);
    }
}
