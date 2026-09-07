using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
/// <summary>
/// Drives the internal scheduler checkpoint boundary without exposing its representation to tests.
/// </summary>
public static class InMemoryOutboxCheckpointDriver
{
    public static int GetPendingMethodCount(object methods)
    {
        ArgumentNullException.ThrowIfNull(methods);
        if (methods is not InMemoryOutboxDeferredMethodCollection deferredMethods)
            throw new ArgumentException("The value must be an in-memory outbox deferred-method collection.", nameof(methods));

        return deferredMethods.CreateCheckpoint();
    }

    public static async Task DiscardActionsCreatedByAttemptAsync(
        object context,
        Func<Task> attempt)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attempt);
        if (context is not InMemoryOutboxMessageSchedulerContext schedulerContext)
            throw new ArgumentException("The value must be an in-memory outbox scheduler context.", nameof(context));

        InMemoryOutboxMessageSchedulerContext.Checkpoint checkpoint = schedulerContext.CreateCheckpoint();
        await attempt().ConfigureAwait(false);
        await schedulerContext.DiscardSinceAsync(checkpoint).ConfigureAwait(false);
    }
}
