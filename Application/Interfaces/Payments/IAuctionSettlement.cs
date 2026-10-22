using Domain.Entities;

namespace Application.Interfaces.Payments
{
    /// <summary>
    /// What happens to the money and to the property after the bidding: who must pay, who is offered the property
    /// when the winner does not pay, whose deposit is kept and whose goes back. Every method runs inside the transaction of
    /// the caller (the clock, a payment callback, a user's answer), so a step and its notifications are saved together.
    /// </summary>
    public interface IAuctionSettlement
    {
        /// <summary>
        /// Right after the result is written: queues refunds for everybody who is not the winner or the runner-up whose deposit is
        /// held, and tells the winner what to pay by when. <paramref name="winnerAward"/> is null when the sale failed.
        /// </summary>
        Task AfterFinishAsync(Auction auction, AuctionAward? winnerAward, CancellationToken cancellationToken = default);

        /// <summary>A deposit payment arrived: the participant may bid. A deposit that cannot be used goes back.</summary>
        Task OnDepositPaidAsync(Payment payment, CancellationToken cancellationToken = default);

        /// <summary>The buyer paid in full: the property is sold and the runner-up's deposit goes back. Late money goes back too.</summary>
        Task OnFinalPaymentPaidAsync(Payment payment, DateTime now, CancellationToken cancellationToken = default);

        /// <summary>A refund was carried out: the deposit is REFUNDED and the participant is told.</summary>
        Task OnRefundPaidAsync(Payment payment, CancellationToken cancellationToken = default);

        /// <summary>The auction was cancelled: everybody who applied is told and paid deposits are queued for refund.</summary>
        Task OnCancelledAsync(Auction auction, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// The buyer did not pay in time (or refused): the deposit is forfeited and the property is offered to the runner-up
        /// at the runner-up's own highest bid, or, with no one left, the sale ends unsold. False when the award is not waiting for payment.
        /// </summary>
        Task<bool> DefaultAsync(Guid awardId, DateTime now, CancellationToken cancellationToken = default);

        /// <summary>The runner-up did not answer in time: counts as declined. False when the offer is not open.</summary>
        Task<bool> ExpireOfferAsync(Guid awardId, DateTime now, CancellationToken cancellationToken = default);

        /// <summary>The runner-up takes the property at their bid; the payment deadline starts.</summary>
        Task AcceptOfferAsync(Guid userId, Guid auctionId, DateTime now, CancellationToken cancellationToken = default);

        /// <summary>The runner-up declines: the deposit goes back and the sale ends unsold.</summary>
        Task DeclineOfferAsync(Guid userId, Guid auctionId, DateTime now, CancellationToken cancellationToken = default);

        /// <summary>The buyer gives up before the deadline: the same as not paying.</summary>
        Task RefuseAsync(Guid userId, Guid auctionId, DateTime now, CancellationToken cancellationToken = default);

        /// <summary>Reminds the buyer once that the payment deadline is near. True when a reminder was queued.</summary>
        Task<bool> RemindPaymentAsync(Guid awardId, DateTime now, CancellationToken cancellationToken = default);
    }
}
