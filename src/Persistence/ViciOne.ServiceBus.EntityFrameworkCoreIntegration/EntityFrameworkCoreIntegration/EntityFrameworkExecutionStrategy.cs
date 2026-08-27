#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

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

    public static Task ExecuteAsync<TMessage>(
        DbContext dbContext,
        IExecutionStrategy strategy,
        ConsumeContext<TMessage> consumeContext,
        Func<Task> operation)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);

        var retryGuard = new OutboxRollbackRetryGuard();

        return ExecuteAsync(
            dbContext,
            strategy,
            () => ExecuteAttemptAsync(consumeContext, operation, retryGuard),
            consumeContext.CancellationToken);
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
                    retryGuard.Block(operationException);
                    LogContext.Warning?.Log(
                        cleanupException,
                        "The failed Entity Framework attempt could not discard every pending outbox action; the business attempt will not be retried.");
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
        private ExceptionDispatchInfo? _blockedFailure;

        public void Block(Exception operationException)
        {
            Interlocked.CompareExchange(
                ref _blockedFailure,
                ExceptionDispatchInfo.Capture(operationException),
                comparand: null);
        }

        public void ThrowIfBlocked()
        {
            Volatile.Read(ref _blockedFailure)?.Throw();
        }
    }
}
