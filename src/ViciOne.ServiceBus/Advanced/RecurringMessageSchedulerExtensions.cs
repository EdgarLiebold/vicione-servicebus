using System;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates recurring-message schedulers for existing send and publish endpoints.</summary>
public static class RecurringMessageSchedulerExtensions
{
    /// <summary>Creates a recurring scheduler that sends commands through the endpoint.</summary>
    /// <param name="endpoint">The endpoint that accepts recurring-schedule commands.</param>
    /// <param name="busTopology">The topology used to resolve recurring-publish destinations.</param>
    /// <param name="timeProvider">The clock used to timestamp control commands.</param>
    /// <returns>A recurring-message scheduler bound to <paramref name="endpoint" />.</returns>
    public static IRecurringMessageScheduler CreateRecurringMessageScheduler(this ISendEndpoint endpoint, IBusTopology? busTopology = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return new EndpointRecurringMessageScheduler(endpoint, busTopology, timeProvider);
    }

    /// <summary>Creates a recurring scheduler that publishes commands through the endpoint.</summary>
    /// <param name="endpoint">The endpoint that publishes recurring-schedule commands.</param>
    /// <param name="busTopology">The topology used to resolve recurring-publish destinations.</param>
    /// <param name="timeProvider">The clock used to timestamp control commands.</param>
    /// <returns>A recurring-message scheduler bound to <paramref name="endpoint" />.</returns>
    public static IRecurringMessageScheduler CreateRecurringMessageScheduler(this IPublishEndpoint endpoint, IBusTopology? busTopology = null,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return new PublishRecurringMessageScheduler(endpoint, busTopology, timeProvider);
    }
}
