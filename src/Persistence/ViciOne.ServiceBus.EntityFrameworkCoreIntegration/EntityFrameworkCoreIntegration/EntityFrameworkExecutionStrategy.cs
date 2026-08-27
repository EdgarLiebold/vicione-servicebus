namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System;
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

        return ExecuteAsync(
            dbContext,
            strategy,
            () => ExecuteAttemptAsync(dbContext, consumeContext, operation),
            consumeContext.CancellationToken);
    }

    private static async Task ExecuteAttemptAsync<TMessage>(
        DbContext dbContext,
        ConsumeContext<TMessage> consumeContext,
        Func<Task> operation)
        where TMessage : class
    {
        OutboxContext outboxContext = consumeContext.TryGetPayload(out InMemoryOutboxConsumeContext inMemoryOutbox)
            ? inMemoryOutbox
            : null;
        OutboxCheckpoint checkpoint = outboxContext?.CreateCheckpoint();

        try
        {
            await operation().ConfigureAwait(false);
        }
        catch
        {
            if (checkpoint is not null)
            {
                try
                {
                    await outboxContext!.DiscardPendingActions(checkpoint).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    LogContext.Warning?.Log(exception, "The failed Entity Framework attempt could not discard every pending outbox action.");
                }
            }

            throw;
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
}
