using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures interval-based admission limits on message pipelines.</summary>
public static class RateLimitConfigurationExtensions
{
    /// <summary>
    /// Adds a rate limiter that admits a fixed number of operations per one-second interval.
    /// </summary>
    /// <typeparam name="T">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to limit.</param>
    /// <param name="rateLimit">The positive number of operations admitted per interval.</param>
    /// <param name="router">An optional control router for runtime limit adjustments.</param>
    public static void UseRateLimit<T>(this IPipeConfigurator<T> configurator, int rateLimit, IPipeRouter? router = null)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(rateLimit, 1);

        var specification = new RateLimitPipeSpecification<T>(rateLimit, TimeSpan.FromSeconds(1), router);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds a rate limiter that starts a usage-anchored interval when the first operation is admitted.
    /// </summary>
    /// <typeparam name="T">The context type carried by the pipeline.</typeparam>
    /// <param name="configurator">The pipeline to limit.</param>
    /// <param name="rateLimit">The positive number of operations admitted per interval.</param>
    /// <param name="interval">The positive duration of each interval.</param>
    /// <param name="router">An optional control router for runtime limit adjustments.</param>
    /// <param name="timeProvider">An optional clock and timer source for interval boundaries.</param>
    public static void UseRateLimit<T>(this IPipeConfigurator<T> configurator, int rateLimit, TimeSpan interval, IPipeRouter? router = null,
        TimeProvider? timeProvider = null)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(rateLimit, 1);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval must be greater than zero.");

        var specification = new RateLimitPipeSpecification<T>(rateLimit, interval, router, timeProvider);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Adds one rate limiter before dispatch so all message types share the same admission budget.
    /// </summary>
    /// <param name="configurator">The consume pipeline to limit.</param>
    /// <param name="rateLimit">The positive number of messages admitted per interval.</param>
    /// <param name="interval">The positive duration of each interval.</param>
    /// <param name="timeProvider">An optional clock and timer source for interval boundaries.</param>
    public static void UseRateLimit(this IConsumePipeConfigurator configurator, int rateLimit, TimeSpan interval, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentOutOfRangeException.ThrowIfLessThan(rateLimit, 1);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), interval, "The interval must be greater than zero.");

        var specification = new RateLimitPipeSpecification<ConsumeContext>(rateLimit, interval, timeProvider: timeProvider);

        configurator.AddPrePipeSpecification(specification);
    }
}
