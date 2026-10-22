using Application.Interfaces.Payments;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services.Payments
{
    public sealed class DepositRefundService(IPaymentRepository payments) : IDepositRefundService
    {
        public async Task<bool> RequestAsync(AuctionParticipant participant, Auction auction, string reason, CancellationToken cancellationToken = default)
        {
            if (participant.DepositStatus != DepositStatus.PAID || auction.DepositAmount <= 0)
                return false;
            if (await payments.AnyAsync(p => p.ParticipantId == participant.Id && p.Purpose == PaymentPurpose.REFUND
                                             && (p.Status == PaymentStatus.PENDING || p.Status == PaymentStatus.PAID), cancellationToken))
                return false;

            // Back the way it came: the staff see which provider took the deposit.
            var deposit = (await payments.GetAllAsync(p => p.ParticipantId == participant.Id && p.Purpose == PaymentPurpose.DEPOSIT && p.Status == PaymentStatus.PAID, cancellationToken))
                          .OrderByDescending(p => p.PaidAt).FirstOrDefault();
            await payments.CreateAsync(new Payment
            {
                UserId = participant.UserId,
                AuctionId = participant.AuctionId,
                ParticipantId = participant.Id,
                Purpose = PaymentPurpose.REFUND,
                Amount = deposit?.Amount ?? auction.DepositAmount,
                Status = PaymentStatus.PENDING,
                Provider = deposit?.Provider ?? PaymentProvider.MANUAL,
                Note = reason,
            }, cancellationToken);
            return true;
        }
    }
}
