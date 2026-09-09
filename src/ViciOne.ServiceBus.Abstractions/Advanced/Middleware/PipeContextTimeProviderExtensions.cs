using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides one context-scoped source of time for message-pipeline decisions. A context without an
/// explicit provider uses <see cref="TimeProvider.System" />.
/// </summary>
public static class PipeContextTimeProviderExtensions
{
    /// <summary>Returns the provider attached to the context, or the system provider when none was attached.</summary>
    /// <param name="context">The pipe context.</param>
    /// <returns>The context-specific or system time provider.</returns>
    public static TimeProvider GetTimeProvider(this PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TryGetPayload<TimeProvider>(out TimeProvider? timeProvider)
            ? timeProvider
            : TimeProvider.System;
    }

    /// <summary>
    /// Returns the current UTC time from the provider attached to the context.
    /// </summary>
    /// <param name="context">The pipe context.</param>
    /// <returns>The provider's current UTC time.</returns>
    public static DateTimeOffset GetUtcNow(this PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.GetTimeProvider().GetUtcNow();
    }

    /// <summary>
    /// Sets the provider used by subsequent time-dependent decisions on this context, keeping all
    /// context timing behind the standard .NET time abstraction.
    /// </summary>
    /// <param name="context">The pipe context.</param>
    /// <param name="timeProvider">The time provider to attach.</param>
    public static void SetTimeProvider(this PipeContext context, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        context.AddOrUpdatePayload<TimeProvider>(() => timeProvider, _ => timeProvider);
    }
}
