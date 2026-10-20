using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Persistance.Data;

namespace Persistance.Services.Repositories.Abstractions
{
    /// <summary>
    /// Scoped per request. Not thread-safe, like the DbContext it wraps.
    /// </summary>
    public sealed class UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
    {
        private readonly List<Func<Task>> _afterCommit = [];
        private readonly List<Func<Task>> _onRollback = [];
        private int _depth;

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            if (_depth > 0)
            {
                _depth++;
                try { return await action(); }
                finally { _depth--; }
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            _depth = 1;
            T result;
            try
            {
                result = await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _depth = 0;
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    // The connection may already be gone; the server drops the transaction anyway.
                    logger.LogWarning(ex, "Transaction rollback failed");
                }

                // Entities saved inside the transaction are tracked as stored although they are not.
                context.ChangeTracker.Clear();
                _afterCommit.Clear();
                await RunAsync(Drain(_onRollback), "rollback");
                throw;
            }

            _depth = 0;
            _onRollback.Clear();
            await RunAsync(Drain(_afterCommit), "after-commit");
            return result;
        }

        public async Task AfterCommitAsync(Func<Task> callback)
        {
            if (_depth > 0)
                _afterCommit.Add(callback);
            else
                await RunAsync([callback], "after-commit");
        }

        public void OnRollback(Func<Task> callback)
        {
            if (_depth > 0)
                _onRollback.Add(callback);
        }

        private static List<Func<Task>> Drain(List<Func<Task>> callbacks)
        {
            var copy = callbacks.ToList();
            callbacks.Clear();
            return copy;
        }

        private async Task RunAsync(List<Func<Task>> callbacks, string stage)
        {
            foreach (var callback in callbacks)
            {
                try
                {
                    await callback();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "A {Stage} step failed", stage);
                }
            }
        }
    }
}
