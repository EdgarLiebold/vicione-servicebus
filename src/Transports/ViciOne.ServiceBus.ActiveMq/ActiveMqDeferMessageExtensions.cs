using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides extension methods for active mq defer message.
/// </summary>
public static class ActiveMqDeferMessageExtensions
{
    /// <summary>
    /// Defers the message for redelivery using a delayed exchange.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="context"></param>
    /// <param name="delay"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task DeferAsync<T>(this ConsumeContext<T> context, TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null, CancellationToken cancellationToken = default)
        where T : class
    {
        MessageRedeliveryContext redeliveryContext = new DelayedMessageRedeliveryContext<T>(context, RedeliveryOptions.None);

        return redeliveryContext.ScheduleRedeliveryAsync(delay, callback, cancellationToken: cancellationToken);
    }
}
