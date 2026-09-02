namespace ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;

using ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>
/// Drives the internal scheduler checkpoint boundary without exposing its representation to tests.
/// </summary>
public static class InMemoryOutboxCheckpointDriver
{
    public static async Task DiscardActionsCreatedByAttempt(
        InMemoryOutboxMessageSchedulerContext context,
        Func<Task> attempt)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(attempt);

        InMemoryOutboxMessageSchedulerContext.Checkpoint checkpoint = context.CreateCheckpoint();
        await attempt().ConfigureAwait(false);
        await context.DiscardSince(checkpoint).ConfigureAwait(false);
    }
}
