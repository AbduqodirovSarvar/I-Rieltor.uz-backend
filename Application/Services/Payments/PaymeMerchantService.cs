using Application.Interfaces.Payments;
using Application.Interfaces.Repositories;
using Application.Models.Configuration;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Application.Services.Payments
{
    /// <summary>
    /// Payme Merchant API (https://developer.help.paycom.uz): CheckPerformTransaction, CreateTransaction, PerformTransaction,
    /// CancelTransaction, CheckTransaction, GetStatement. The order is our payment (<c>account.order_id</c> = its Id), the amount
    /// is in tiyin. A payment can be cancelled by Payme only while it is not yet performed: money already received goes
    /// back through our own refund flow (-31007 tells Payme so).
    /// </summary>
    public sealed class PaymeMerchantService(
        IPaymentRepository payments,
        IPaymentProcessor processor,
        PaymentSettings settings,
        IUnitOfWork unitOfWork,
        ILogger<PaymeMerchantService> logger) : IPaymeMerchantService
    {
        // Payme error codes.
        private const int InsufficientPrivileges = -32504;
        private const int MethodNotFound = -32601;
        private const int InvalidRequest = -32600;
        private const int SystemError = -32400;
        private const int WrongAmount = -31001;
        private const int TransactionNotFound = -31003;
        private const int CannotCancel = -31007;
        private const int CannotPerform = -31008;
        private const int OrderNotFound = -31050;
        private const int OrderNotPayable = -31051;
        private const int OrderBusy = -31099;

        // A transaction that stays "created" this long is cancelled (reason 4).
        private static readonly TimeSpan TransactionTimeout = TimeSpan.FromHours(12);
        private const int TimeoutReason = 4;

        private sealed class PaymeException(int code, string english, string? data = null) : Exception(english)
        {
            public int Code { get; } = code;
            public string? Data { get; } = data;
        }

        public async Task<JsonObject> HandleAsync(JsonElement request, string? authorizationHeader, CancellationToken cancellationToken = default)
        {
            JsonNode? id = null;
            try
            {
                if (request.ValueKind != JsonValueKind.Object)
                    throw new PaymeException(InvalidRequest, "Invalid request.");
                if (request.TryGetProperty("id", out var idElement))
                    id = JsonNode.Parse(idElement.GetRawText());

                if (!settings.Payme.Enabled || !IsAuthorized(authorizationHeader))
                    throw new PaymeException(InsufficientPrivileges, "Insufficient privileges to perform this method.");

                var method = request.TryGetProperty("method", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
                var parameters = request.TryGetProperty("params", out var p) && p.ValueKind == JsonValueKind.Object ? p : default;
                if (parameters.ValueKind != JsonValueKind.Object)
                    throw new PaymeException(InvalidRequest, "Invalid request.");

                var result = method switch
                {
                    "CheckPerformTransaction" => await CheckPerformAsync(parameters, cancellationToken),
                    "CreateTransaction" => await CreateAsync(parameters, cancellationToken),
                    "PerformTransaction" => await PerformAsync(parameters, cancellationToken),
                    "CancelTransaction" => await CancelAsync(parameters, cancellationToken),
                    "CheckTransaction" => await CheckAsync(parameters, cancellationToken),
                    "GetStatement" => await StatementAsync(parameters, cancellationToken),
                    _ => throw new PaymeException(MethodNotFound, "Method not found."),
                };
                return new JsonObject { ["result"] = result, ["id"] = id };
            }
            catch (PaymeException ex)
            {
                return Error(id, ex);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Whatever went wrong inside, Payme must get a well-formed answer; it retries on -32400.
                logger.LogError(ex, "Payme request failed");
                return Error(id, new PaymeException(SystemError, "System error."));
            }
        }

        // ---------------- methods

        private async Task<JsonObject> CheckPerformAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            await FindPayablePaymentAsync(parameters, cancellationToken);
            return new JsonObject { ["allow"] = true };
        }

        private async Task<JsonObject> CreateAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            var transactionId = RequiredString(parameters, "id");
            var existing = await payments.GetAsync(p => p.Provider == PaymentProvider.PAYME && p.ProviderTransactionId == transactionId, cancellationToken);
            if (existing != null)
            {
                if (StateOf(existing) != 1)
                    throw new PaymeException(CannotPerform, "The transaction cannot be performed.");
                if (IsExpired(existing))
                {
                    await CancelByTimeoutAsync(existing, cancellationToken);
                    throw new PaymeException(CannotPerform, "The transaction has timed out.");
                }
                return Created(existing);
            }

            var payment = await FindPayablePaymentAsync(parameters, cancellationToken);
            // One transaction at a time per order: while another is waiting, a second cannot start.
            if (payment.ProviderTransactionId != null)
                throw new PaymeException(OrderBusy, "The order is waiting for payment.", "order_id");

            payment.ProviderTransactionId = transactionId;
            payment.ProviderCreatedAt = FromMilliseconds(RequiredLong(parameters, "time"));
            await payments.UpdateAsync(payment, cancellationToken);
            return Created(payment);
        }

        private async Task<JsonObject> PerformAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            var transactionId = RequiredString(parameters, "id");
            // Payme may repeat the call at once; the row version lets one settle the payment and the repeat see the result.
            for (var attempt = 0; ; attempt++)
            {
                var payment = await FindTransactionAsync(transactionId, cancellationToken);
                switch (StateOf(payment))
                {
                    case 2:
                        return Performed(payment);
                    case 1:
                        if (IsExpired(payment))
                        {
                            await CancelByTimeoutAsync(payment, cancellationToken);
                            throw new PaymeException(CannotPerform, "The transaction has timed out.");
                        }
                        try
                        {
                            await unitOfWork.ExecuteAsync(async () =>
                            {
                                await processor.MarkPaidAsync(payment, DateTime.UtcNow, cancellationToken);
                                return true;
                            }, cancellationToken);
                            return Performed(payment);
                        }
                        catch (DbUpdateConcurrencyException) when (attempt < 2)
                        {
                            continue;
                        }
                    default:
                        throw new PaymeException(CannotPerform, "The transaction cannot be performed.");
                }
            }
        }

        private async Task<JsonObject> CancelAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            var payment = await FindTransactionAsync(RequiredString(parameters, "id"), cancellationToken);
            switch (StateOf(payment))
            {
                case 1:
                    payment.Status = PaymentStatus.CANCELLED;
                    payment.CancelledAt = DateTime.UtcNow;
                    payment.ProviderReason = (int?)RequiredLong(parameters, "reason");
                    await payments.UpdateAsync(payment, cancellationToken);
                    return Cancelled(payment);
                case 2:
                    // The deposit or the price is already counted. Money received is returned by our refund flow, not by Payme's cancel.
                    throw new PaymeException(CannotCancel, "The order is completed and cannot be cancelled.");
                default:
                    return Cancelled(payment);
            }
        }

        private async Task<JsonObject> CheckAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            var payment = await FindTransactionAsync(RequiredString(parameters, "id"), cancellationToken);
            return new JsonObject
            {
                ["create_time"] = Milliseconds(payment.ProviderCreatedAt),
                ["perform_time"] = Milliseconds(payment.PaidAt),
                ["cancel_time"] = Milliseconds(payment.CancelledAt),
                ["transaction"] = payment.Id.ToString("N"),
                ["state"] = StateOf(payment),
                ["reason"] = payment.ProviderReason,
            };
        }

        private async Task<JsonObject> StatementAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            var from = FromMilliseconds(RequiredLong(parameters, "from"));
            var to = FromMilliseconds(RequiredLong(parameters, "to"));
            var list = await payments.GetAllAsync(p => p.Provider == PaymentProvider.PAYME && p.ProviderTransactionId != null
                                                       && p.ProviderCreatedAt >= from && p.ProviderCreatedAt <= to, cancellationToken);
            var transactions = new JsonArray();
            foreach (var payment in list.OrderBy(p => p.ProviderCreatedAt))
                transactions.Add(new JsonObject
                {
                    ["id"] = payment.ProviderTransactionId,
                    ["time"] = Milliseconds(payment.ProviderCreatedAt),
                    ["amount"] = Tiyin(payment),
                    ["account"] = new JsonObject { ["order_id"] = payment.Id.ToString() },
                    ["create_time"] = Milliseconds(payment.ProviderCreatedAt),
                    ["perform_time"] = Milliseconds(payment.PaidAt),
                    ["cancel_time"] = Milliseconds(payment.CancelledAt),
                    ["transaction"] = payment.Id.ToString("N"),
                    ["state"] = StateOf(payment),
                    ["reason"] = payment.ProviderReason,
                });
            return new JsonObject { ["transactions"] = transactions };
        }

        // ---------------- helpers

        /// <summary>The order exists, is ours to be paid through Payme, is still unpaid, and the amount is exactly right.</summary>
        private async Task<Payment> FindPayablePaymentAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            var orderId = parameters.TryGetProperty("account", out var account) && account.ValueKind == JsonValueKind.Object
                          && account.TryGetProperty("order_id", out var order) && order.ValueKind == JsonValueKind.String
                          && Guid.TryParse(order.GetString(), out var parsed) ? parsed : (Guid?)null;
            var payment = orderId == null ? null : await payments.GetAsync(p => p.Id == orderId, cancellationToken);
            if (payment == null)
                throw new PaymeException(OrderNotFound, "Order not found.", "order_id");
            if (payment.Provider != PaymentProvider.PAYME || payment.Status != PaymentStatus.PENDING || payment.Purpose == PaymentPurpose.REFUND)
                throw new PaymeException(OrderNotPayable, "The order cannot be paid.", "order_id");
            if (RequiredLong(parameters, "amount") != Tiyin(payment))
                throw new PaymeException(WrongAmount, "Wrong amount.", "amount");
            return payment;
        }

        private async Task<Payment> FindTransactionAsync(string transactionId, CancellationToken cancellationToken)
            => await payments.GetAsync(p => p.Provider == PaymentProvider.PAYME && p.ProviderTransactionId == transactionId, cancellationToken)
               ?? throw new PaymeException(TransactionNotFound, "Transaction not found.");

        private async Task CancelByTimeoutAsync(Payment payment, CancellationToken cancellationToken)
        {
            payment.Status = PaymentStatus.CANCELLED;
            payment.CancelledAt = DateTime.UtcNow;
            payment.ProviderReason = TimeoutReason;
            await payments.UpdateAsync(payment, cancellationToken);
        }

        private static bool IsExpired(Payment payment)
            => payment.ProviderCreatedAt is { } created && DateTime.UtcNow - created > TransactionTimeout;

        /// <summary>Payme states: 1 created, 2 performed, -1 cancelled before performing, -2 cancelled after (not used here).</summary>
        private static int StateOf(Payment payment) => payment.Status switch
        {
            PaymentStatus.PENDING => 1,
            PaymentStatus.PAID => 2,
            PaymentStatus.CANCELLED => -1,
            PaymentStatus.REFUNDED => -2,
            _ => 0,
        };

        private static JsonObject Created(Payment payment) => new()
        {
            ["create_time"] = Milliseconds(payment.ProviderCreatedAt),
            ["transaction"] = payment.Id.ToString("N"),
            ["state"] = StateOf(payment),
        };

        private static JsonObject Performed(Payment payment) => new()
        {
            ["transaction"] = payment.Id.ToString("N"),
            ["perform_time"] = Milliseconds(payment.PaidAt),
            ["state"] = StateOf(payment),
        };

        private static JsonObject Cancelled(Payment payment) => new()
        {
            ["transaction"] = payment.Id.ToString("N"),
            ["cancel_time"] = Milliseconds(payment.CancelledAt),
            ["state"] = StateOf(payment),
        };

        private static long Tiyin(Payment payment) => (long)Math.Round(payment.Amount * 100, MidpointRounding.AwayFromZero);

        private static long Milliseconds(DateTime? time) => time is { } t ? new DateTimeOffset(DateTime.SpecifyKind(t, DateTimeKind.Utc)).ToUnixTimeMilliseconds() : 0;

        private static DateTime FromMilliseconds(long milliseconds) => DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;

        private static string RequiredString(JsonElement parameters, string name)
            => parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(value.GetString())
                ? value.GetString()!
                : throw new PaymeException(InvalidRequest, $"Parameter '{name}' is required.");

        private static long RequiredLong(JsonElement parameters, string name)
            => parameters.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)
                ? number
                : throw new PaymeException(InvalidRequest, $"Parameter '{name}' is required.");

        /// <summary>Basic base64("Paycom:" + key), compared without leaking where the first difference is.</summary>
        private bool IsAuthorized(string? header)
        {
            if (string.IsNullOrEmpty(header) || !header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
                return false;
            var expected = Encoding.UTF8.GetBytes("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("Paycom:" + settings.Payme.Key)));
            var given = Encoding.UTF8.GetBytes(header.Trim());
            return given.Length == expected.Length && CryptographicOperations.FixedTimeEquals(given, expected);
        }

        private static JsonObject Error(JsonNode? id, PaymeException ex) => new()
        {
            ["error"] = new JsonObject
            {
                ["code"] = ex.Code,
                // Payme shows the message to the payer in their language; the same text in all three is fine for a technical error.
                ["message"] = new JsonObject { ["ru"] = ex.Message, ["uz"] = ex.Message, ["en"] = ex.Message },
                ["data"] = ex.Data,
            },
            ["id"] = id,
        };
    }
}
