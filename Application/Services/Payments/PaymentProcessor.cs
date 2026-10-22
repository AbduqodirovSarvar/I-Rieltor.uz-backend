using Application.Interfaces.Payments;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Payments
{
    public sealed class PaymentProcessor(IPaymentRepository payments, IAuctionSettlement settlement) : IPaymentProcessor
    {
        public async Task MarkPaidAsync(Payment payment, DateTime now, CancellationToken cancellationToken = default)
        {
            payment.Status = PaymentStatus.PAID;
            payment.PaidAt = now;
            // The row version makes the same payment reported twice at once settle only once: the second save fails.
            await payments.UpdateAsync(payment, cancellationToken);

            switch (payment.Purpose)
            {
                case PaymentPurpose.DEPOSIT:
                    await settlement.OnDepositPaidAsync(payment, cancellationToken);
                    break;
                case PaymentPurpose.FINAL_PAYMENT:
                    await settlement.OnFinalPaymentPaidAsync(payment, now, cancellationToken);
                    break;
                case PaymentPurpose.REFUND:
                    await settlement.OnRefundPaidAsync(payment, cancellationToken);
                    break;
            }
        }
    }
}
