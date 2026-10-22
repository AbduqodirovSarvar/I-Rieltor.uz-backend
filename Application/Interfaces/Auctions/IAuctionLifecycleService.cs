namespace Application.Interfaces.Auctions
{
    /// <param name="Started">Auctions that went SCHEDULED → ONGOING in this run.</param>
    /// <param name="Finished">Auctions that went ONGOING → FINISHED or FAILED in this run.</param>
    /// <param name="Reminded">Auctions whose participants were reminded that the sale starts soon.</param>
    /// <param name="Defaulted">Buyers who missed their payment deadline.</param>
    /// <param name="OffersExpired">Runner-up offers that were not answered in time.</param>
    /// <param name="PaymentsReminded">Buyers reminded that their payment deadline is near.</param>
    public sealed record AuctionLifecycleRun(int Started, int Finished, int Reminded = 0, int Defaulted = 0, int OffersExpired = 0, int PaymentsReminded = 0);

    public interface IAuctionLifecycleService
    {
        /// <summary>
        /// Does what is due at <paramref name="now"/> (UTC). The clock is a parameter, so tests can move time.
        /// </summary>
        Task<AuctionLifecycleRun> RunOnceAsync(DateTime now, CancellationToken cancellationToken = default);
    }
}
