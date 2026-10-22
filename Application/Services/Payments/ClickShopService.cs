using Application.Interfaces.Payments;
using Application.Interfaces.Repositories;
using Application.Models.Configuration;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Application.Services.Payments
{
    /// <summary>
    /// Click SHOP-API (docs.click.uz): Click calls <c>prepare</c> (action 0) when the payer confirms, then <c>complete</c> (action 1)
    /// when Click has the money. Every call carries <c>sign_string</c> = MD5 of its fields and the service secret. The order is our
    /// payment (<c>merchant_trans_id</c> = its Id); <c>merchant_prepare_id</c> is a number we choose: the Unix milliseconds of the prepare.
    /// </summary>
    public sealed class ClickShopService(
        IPaymentRepository payments,
        IPaymentProcessor processor,
        PaymentSettings settings,
        IUnitOfWork unitOfWork,
        ILogger<ClickShopService> logger) : IClickShopService
    {
        private const int Success = 0;
        private const int SignCheckFailed = -1;
        private const int IncorrectAmount = -2;
        private const int ActionNotFound = -3;
        private const int AlreadyPaid = -4;
        private const int OrderNotFound = -5;
        private const int TransactionNotFound = -6;
        private const int RequestError = -8;
        private const int TransactionCancelled = -9;

        public async Task<JsonObject> HandleAsync(ClickStep step, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken = default)
        {
            string Field(string name) => form.TryGetValue(name, out var value) ? value : string.Empty;
            var clickTransId = Field("click_trans_id");
            var merchantTransId = Field("merchant_trans_id");
            JsonObject Answer(int error, string note, long? prepareId = null)
            {
                var answer = new JsonObject
                {
                    ["click_trans_id"] = clickTransId,
                    ["merchant_trans_id"] = merchantTransId,
                    [step == ClickStep.Prepare ? "merchant_prepare_id" : "merchant_confirm_id"] = prepareId,
                    ["error"] = error,
                    ["error_note"] = note,
                };
                return answer;
            }

            try
            {
                if (!settings.Click.Enabled || !SignatureIsValid(step, Field))
                    return Answer(SignCheckFailed, "SIGN CHECK FAILED!");
                if (Field("service_id") != settings.Click.ServiceId)
                    return Answer(RequestError, "Error in request from click");
                if (Field("action") != (step == ClickStep.Prepare ? "0" : "1"))
                    return Answer(ActionNotFound, "Action not found");

                var payment = Guid.TryParse(merchantTransId, out var orderId)
                    ? await payments.GetAsync(p => p.Id == orderId && p.Provider == PaymentProvider.CLICK && p.Purpose != PaymentPurpose.REFUND, cancellationToken)
                    : null;
                if (payment == null)
                    return Answer(OrderNotFound, "Order not found");
                if (!decimal.TryParse(Field("amount"), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || Math.Abs(amount - payment.Amount) >= 0.01m)
                    return Answer(IncorrectAmount, "Incorrect parameter amount");
                if (payment.Status == PaymentStatus.PAID)
                    return Answer(AlreadyPaid, "Already paid");
                if (payment.Status != PaymentStatus.PENDING)
                    return Answer(TransactionCancelled, "Transaction cancelled");

                return step == ClickStep.Prepare
                    ? await PrepareAsync(payment, clickTransId, Answer, cancellationToken)
                    : await CompleteAsync(payment, clickTransId, Field("merchant_prepare_id"), Field("error"), Answer, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // The same complete arrived twice at once and the other one won: ask again and Click is told it is paid.
                return Answer(AlreadyPaid, "Already paid");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Click {Step} failed", step);
                return Answer(RequestError, "Error in request from click");
            }
        }

        private async Task<JsonObject> PrepareAsync(Payment payment, string clickTransId, Func<int, string, long?, JsonObject> answer, CancellationToken cancellationToken)
        {
            // A repeated prepare of the same Click transaction keeps its id; a new attempt gets a new one.
            if (payment.ProviderTransactionId != clickTransId || payment.ProviderCreatedAt == null)
            {
                payment.ProviderTransactionId = clickTransId;
                payment.ProviderCreatedAt = DateTime.UtcNow;
                await payments.UpdateAsync(payment, cancellationToken);
            }
            return answer(Success, "Success", PrepareId(payment));
        }

        private async Task<JsonObject> CompleteAsync(Payment payment, string clickTransId, string prepareId, string error, Func<int, string, long?, JsonObject> answer, CancellationToken cancellationToken)
        {
            if (payment.ProviderTransactionId != clickTransId || prepareId != PrepareId(payment).ToString(CultureInfo.InvariantCulture))
                return answer(TransactionNotFound, "Transaction not found", null);

            // Click could not take the money from the payer.
            if (int.TryParse(error, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code) && code < 0)
            {
                payment.Status = PaymentStatus.CANCELLED;
                payment.CancelledAt = DateTime.UtcNow;
                payment.ProviderReason = code;
                await payments.UpdateAsync(payment, cancellationToken);
                return answer(TransactionCancelled, "Transaction cancelled", null);
            }

            await unitOfWork.ExecuteAsync(async () =>
            {
                await processor.MarkPaidAsync(payment, DateTime.UtcNow, cancellationToken);
                return true;
            }, cancellationToken);
            return answer(Success, "Success", PrepareId(payment));
        }

        private static long PrepareId(Payment payment)
            => new DateTimeOffset(DateTime.SpecifyKind(payment.ProviderCreatedAt ?? DateTime.UtcNow, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

        /// <summary>
        /// prepare: md5(click_trans_id + service_id + secret + merchant_trans_id + amount + action + sign_time);
        /// complete: the same with merchant_prepare_id after merchant_trans_id. Strings exactly as Click sent them.
        /// </summary>
        private bool SignatureIsValid(ClickStep step, Func<string, string> field)
        {
            var text = field("click_trans_id") + field("service_id") + settings.Click.SecretKey + field("merchant_trans_id")
                       + (step == ClickStep.Complete ? field("merchant_prepare_id") : string.Empty)
                       + field("amount") + field("action") + field("sign_time");
            var expected = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
            var given = Encoding.ASCII.GetBytes(field("sign_string").ToLowerInvariant());
            return given.Length == expected.Length && CryptographicOperations.FixedTimeEquals(given, Encoding.ASCII.GetBytes(expected));
        }
    }
}
