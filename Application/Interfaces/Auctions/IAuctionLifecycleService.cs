namespace Application.Interfaces.Auctions
{
    /// <param name="Started">Auctions that went SCHEDULED → ONGOING in this run.</param>
    /// <param name="Finished">Auctions that went ONGOING → FINISHED or FAILED in this run.</param>
    public sealed record AuctionLifecycleRun(int Started, int Finished);

    public interface IAuctionLifecycleService
    {
        /// <summary>
        /// Does what is due at <paramref name="now"/> (UTC). The clock is a parameter, so tests can move time.
        /// </summary>
        Task<AuctionLifecycleRun> RunOnceAsync(DateTime now, CancellationToken cancellationToken = default);
    }
}
