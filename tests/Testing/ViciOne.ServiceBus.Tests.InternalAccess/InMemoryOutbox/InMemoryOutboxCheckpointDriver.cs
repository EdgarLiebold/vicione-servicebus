using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

namespace ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
/// <summary>
/// Drives the internal scheduler checkpoint boundary without exposing its representation to tests.
/// </summary>
public static class InMemoryOutboxCheckpointDriver
{
    public static int GetPendingMethodCount(InMemoryOutboxDeferredMethodCollection methods)
    {
        ArgumentNullException.ThrowIfNull(methods);

        return methods.CreateCheckpoint();
    }

    public static async Task DiscardActionsCreatedByAttemptAsync(
        InMemoryOutboxMessageSchedulerContext context,
        Func<Task> attempt)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attempt);

        InMemoryOutboxMessageSchedulerContext.Checkpoint checkpoint = context.CreateCheckpoint();
        await attempt().ConfigureAwait(false);
        await context.DiscardSinceAsync(checkpoint).ConfigureAwait(false);
    }
}
