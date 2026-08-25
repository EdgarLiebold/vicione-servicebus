namespace ViciOne.ServiceBus;

using System;


/// <summary>
/// Provides one context-scoped source of time for message-pipeline decisions. A context without an
/// explicit provider uses <see cref="TimeProvider.System" />.
/// </summary>
public static class PipeContextTimeProviderExtensions
{
    /// <summary>
    /// Returns the provider attached to the context, or the system provider when none was attached.
    /// </summary>
    public static TimeProvider GetTimeProvider(this PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.TryGetPayload<TimeProvider>(out TimeProvider? timeProvider)
            ? timeProvider
            : TimeProvider.System;
    }

    /// <summary>
    /// Sets the provider used by subsequent time-dependent decisions on this context. Middleware and
    /// deterministic tests can use the standard .NET abstraction without introducing another clock.
    /// </summary>
    public static void SetTimeProvider(this PipeContext context, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        context.AddOrUpdatePayload<TimeProvider>(() => timeProvider, _ => timeProvider);
    }
}
