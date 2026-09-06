using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for redeliver.</summary>
public static class RedeliverExtensions
{
    /// <summary>
    /// Redeliver uses the message scheduler to deliver the message to the queue at a future
    /// time. The delivery count is incremented. Moreover, if you give custom callback action, it perform before sending message to queue.
    /// A message scheduler must be configured on the bus for redelivery to be enabled.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context of the message.</param>
    /// <param name="delay">The delay before the message is delivered. It may take longer to receive the message if the queue is not empty.</param>
    /// <param name="callback">Operation which is executed before the message is delivered.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task RedeliverAsync<T>(this ConsumeContext<T> context, TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!context.TryGetPayload(out MessageRedeliveryContext? redeliveryContext))
            redeliveryContext = new ScheduleMessageRedeliveryContext<T>(context, RedeliveryOptions.ReplaceMessageId);

        return redeliveryContext.ScheduleRedeliveryAsync(delay, callback, cancellationToken: cancellationToken);
    }
}
