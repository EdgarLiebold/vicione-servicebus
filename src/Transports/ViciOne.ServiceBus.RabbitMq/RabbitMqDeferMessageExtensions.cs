using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Schedules RabbitMQ redelivery through the delayed-message exchange plug-in.</summary>
public static class RabbitMqDeferMessageExtensions
{
    /// <summary>Defers the current message through a delayed exchange declared by the RabbitMQ plug-in.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The consumed message context.</param>
    /// <param name="delay">The delay before redelivery.</param>
    /// <param name="callback">An optional callback that customizes the redelivery send context.</param>
    /// <param name="cancellationToken">Cancellation for scheduling the redelivery.</param>
    /// <returns>A task that completes when the delayed redelivery has been scheduled.</returns>
    public static Task DeferAsync<T>(this ConsumeContext<T> context, TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null, CancellationToken cancellationToken = default)
        where T : class
    {
        MessageRedeliveryContext redeliveryContext = new DelayedMessageRedeliveryContext<T>(context, RedeliveryOptions.None);

        return redeliveryContext.ScheduleRedeliveryAsync(delay, callback, cancellationToken: cancellationToken);
    }
}
