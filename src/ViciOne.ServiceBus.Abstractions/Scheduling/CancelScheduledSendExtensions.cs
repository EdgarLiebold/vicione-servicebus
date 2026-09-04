using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for cancel scheduled send.
/// </summary>
public static class CancelScheduledSendExtensions
{
    /// <summary>
    /// Cancel a scheduled message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="scheduler">The message scheduler</param>
    /// <param name="message">The </param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task CancelScheduledSendAsync<T>(this IMessageScheduler scheduler, ScheduledMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        if (scheduler == null)
            throw new ArgumentNullException(nameof(scheduler));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        return scheduler.CancelScheduledSendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Cancel a scheduled message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context">The message scheduler</param>
    /// <param name="message">The </param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task CancelScheduledSendAsync<T>(this ConsumeContext context, ScheduledMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.CancelScheduledSendAsync(message, cancellationToken);
    }
}
