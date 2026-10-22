using Domain.Entities;

namespace Application.Interfaces.Payments
{
    public interface IDepositRefundService
    {
        /// <summary>
        /// Queues the return of the participant's paid deposit: a REFUND payment in PENDING for the staff (or the payment
        /// provider's refund) to carry out. Nothing happens when no deposit was paid or a refund was already requested.
        /// Call it inside the transaction of the event that releases the deposit.
        /// </summary>
        /// <returns>True when a refund request was created.</returns>
        Task<bool> RequestAsync(AuctionParticipant participant, Auction auction, string reason, CancellationToken cancellationToken = default);
    }
}
