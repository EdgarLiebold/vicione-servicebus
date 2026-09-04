using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

internal static class EntityFrameworkExecutionStrategy
{
    public static async Task ExecuteAsync(
        DbContext dbContext,
        IExecutionStrategy strategy,
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteAsync(dbContext, strategy, async () =>
        {
            await operation().ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    public static Task<TResult> ExecuteAsync<TResult>(
        DbContext dbContext,
        IExecutionStrategy strategy,
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(operation);

        return strategy.ExecuteAsync(
            _ => ExecuteAttemptAsync(dbContext, operation),
            cancellationToken);
    }

    public static async Task ExecuteAsync<TMessage>(
        DbContext dbContext,
        IExecutionStrategy strategy,
        ConsumeContext<TMessage> consumeContext,
        Func<Task> operation)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);

        var retryGuard = new OutboxRollbackRetryGuard();

        try
        {
            await ExecuteAsync(
                    dbContext,
                    strategy,
                    () => ExecuteAttemptAsync(consumeContext, operation, retryGuard),
                    consumeContext.CancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception) when (retryGuard.IsBlocked)
        {
            // A provider strategy may wrap its last delegate failure (for example in
            // RetryLimitExceededException). Once rollback cleanup failed, the first business
            // failure remains authoritative and must cross the strategy boundary unchanged.
            retryGuard.ThrowOriginalFailure();
            throw;
        }

        // A custom strategy is allowed to consume a delegate exception and return. It must not be
        // able to turn an attempt with an incomplete outbox rollback into a successful operation.
        retryGuard.ThrowOriginalFailure();
    }

    private static async Task ExecuteAttemptAsync<TMessage>(
        ConsumeContext<TMessage> consumeContext,
        Func<Task> operation,
        OutboxRollbackRetryGuard retryGuard)
        where TMessage : class
    {
        retryGuard.ThrowIfBlocked();

        OutboxContext? outboxContext = consumeContext.TryGetPayload(out OutboxContext? inMemoryOutbox)
            ? inMemoryOutbox
            : null;
        OutboxCheckpoint? checkpoint = outboxContext?.CreateCheckpoint();

        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception operationException)
        {
            if (checkpoint is not null)
            {
                try
                {
                    await outboxContext!.DiscardPendingActions(checkpoint).ConfigureAwait(false);
                }
                catch (Exception cleanupException)
                {
                    retryGuard.Block(operationException, cleanupException);
                    LogContext.Warning?.Log(
                        cleanupException,
                        "The failed Entity Framework attempt could not discard every pending outbox action; the business attempt will not be retried.");

                    throw new OutboxRollbackRetryStoppedException(cleanupException);
                }
            }

            ExceptionDispatchInfo.Capture(operationException).Throw();
            throw new InvalidOperationException("The captured operation exception was not rethrown.");
        }
    }

    private static async Task<TResult> ExecuteAttemptAsync<TResult>(DbContext dbContext, Func<Task<TResult>> operation)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch
        {
            // Execution strategies may call the operation again on this DbContext. EF does not undo
            // tracked entity state when a database transaction rolls back, so a retry must begin
            // without entities from the failed unit of work.
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private sealed class OutboxRollbackRetryGuard
    {
        private BlockedFailure? _blockedFailure;

        public bool IsBlocked => Volatile.Read(ref _blockedFailure) is not null;

        public void Block(Exception operationException, Exception cleanupException)
        {
            Interlocked.CompareExchange(
                ref _blockedFailure,
                new BlockedFailure(
                    ExceptionDispatchInfo.Capture(operationException),
                    ExceptionDispatchInfo.Capture(cleanupException)),
                comparand: null);
        }

        public void ThrowIfBlocked()
        {
            BlockedFailure? blockedFailure = Volatile.Read(ref _blockedFailure);
            if (blockedFailure is not null)
                throw new OutboxRollbackRetryStoppedException(blockedFailure.CleanupFailure.SourceException);
        }

        public void ThrowOriginalFailure()
        {
            Volatile.Read(ref _blockedFailure)?.OperationFailure.Throw();
        }

        private sealed record BlockedFailure(
            ExceptionDispatchInfo OperationFailure,
            ExceptionDispatchInfo CleanupFailure);
    }

    private sealed class OutboxRollbackRetryStoppedException(Exception cleanupFailure)
        : Exception("The failed outbox attempt cannot be retried because rollback cleanup did not complete.", cleanupFailure);
}
