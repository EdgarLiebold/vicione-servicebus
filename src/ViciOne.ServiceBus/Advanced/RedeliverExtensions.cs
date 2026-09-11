using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Schedules redelivery of the current consumed message.</summary>
public static class RedeliverExtensions
{
    /// <summary>
    /// Schedules the consumed message for redelivery, increments its delivery count and applies an
    /// optional send-context callback before scheduling.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context of the message.</param>
    /// <param name="delay">The delay before the message is delivered. It may take longer to receive the message if the queue is not empty.</param>
    /// <param name="callback">An optional callback that configures the scheduled send.</param>
    /// <param name="cancellationToken">Cancels scheduling.</param>
    /// <returns>A task that completes when the scheduler accepts the redelivery.</returns>
    public static Task RedeliverAsync<T>(this ConsumeContext<T> context, TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.TryGetPayload(out MessageRedeliveryContext? redeliveryContext))
            redeliveryContext = new ScheduleMessageRedeliveryContext<T>(context, RedeliveryOptions.ReplaceMessageId);

        return redeliveryContext.ScheduleRedeliveryAsync(delay, callback, cancellationToken: cancellationToken);
    }
}
