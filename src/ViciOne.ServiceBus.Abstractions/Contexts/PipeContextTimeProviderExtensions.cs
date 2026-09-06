using System;

namespace ViciOne.ServiceBus.Advanced;
/// <summary>
/// Provides one context-scoped source of time for message-pipeline decisions. A context without an
/// explicit provider uses <see cref="TimeProvider.System" />.
/// </summary>
public static class PipeContextTimeProviderExtensions
{
    /// <summary>Returns the provider attached to the context, or the system provider when none was attached.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The time provider.</returns>
    public static TimeProvider GetTimeProvider(this PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TryGetPayload<TimeProvider>(out TimeProvider? timeProvider)
            ? timeProvider
            : TimeProvider.System;
    }

    /// <summary>
    /// Returns the current UTC timestamp from the provider attached to the context. Message features
    /// use this method as their sole fallback when the transport did not supply a sent timestamp.
    /// </summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The utc date time.</returns>
    public static DateTimeOffset GetUtcDateTime(this PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetTimeProvider().GetUtcNow().UtcDateTime;
    }

    /// <summary>
    /// Sets the provider used by subsequent time-dependent decisions on this context, keeping all
    /// context timing behind the standard .NET clock abstraction.
    /// </summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public static void SetTimeProvider(this PipeContext context, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        context.AddOrUpdatePayload<TimeProvider>(() => timeProvider, _ => timeProvider);
    }
}
