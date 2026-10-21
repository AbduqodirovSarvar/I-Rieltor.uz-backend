namespace Application.Interfaces.Repositories
{
    /// <summary>
    /// One database transaction per business operation, plus side effects outside the database
    /// (files on disk, cache) that must follow its outcome.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Runs <paramref name="action"/> in one transaction: every SaveChanges inside commits together or not at all.
        /// A call inside an already running operation joins it instead of opening a second transaction.
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Queues work to run only once the transaction has committed (e.g. moving a replaced file away,
        /// writing the cache). Outside a transaction it runs at once. Its failure is logged, never thrown:
        /// the data is already saved.
        /// </summary>
        Task AfterCommitAsync(Func<Task> callback);

        /// <summary>
        /// Queues undo work for a side effect already done (e.g. deleting a file just written to disk)
        /// in case the transaction rolls back. Outside a transaction there is nothing to undo, so it is ignored.
        /// </summary>
        void OnRollback(Func<Task> callback);

        /// <summary>
        /// Queues work for when COMMIT fails and nobody knows whether it went through (e.g. dropping a cache key, which
        /// is right either way). Outside a transaction it is ignored. Its failure is logged, never thrown.
        /// </summary>
        void OnCommitOutcomeUnknown(Func<Task> callback);
    }
}
