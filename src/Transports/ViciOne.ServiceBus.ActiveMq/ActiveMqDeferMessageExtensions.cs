using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Provides broker-scheduled redelivery for messages consumed from ActiveMQ.</summary>
public static class ActiveMqDeferMessageExtensions
{
    /// <summary>Defers the message for broker-scheduled redelivery.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The message consumption context.</param>
    /// <param name="delay">The delay before broker-scheduled redelivery.</param>
    /// <param name="callback">An optional callback that configures the redelivery send context.</param>
    /// <param name="cancellationToken">The token used to cancel scheduling.</param>
    /// <returns>A task that completes when redelivery has been scheduled.</returns>
    public static Task DeferAsync<T>(this ConsumeContext<T> context, TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null, CancellationToken cancellationToken = default)
        where T : class
    {
        MessageRedeliveryContext redeliveryContext = new DelayedMessageRedeliveryContext<T>(context, RedeliveryOptions.None);

        return redeliveryContext.ScheduleRedeliveryAsync(delay, callback, cancellationToken: cancellationToken);
    }
}
