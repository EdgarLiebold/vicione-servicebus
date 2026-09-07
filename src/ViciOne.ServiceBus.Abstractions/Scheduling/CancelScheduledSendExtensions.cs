using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides cancellation overloads for scheduled messages.</summary>
public static class CancelScheduledSendExtensions
{
    /// <summary>Cancels a scheduled message through its originating scheduler.</summary>
    /// <typeparam name="T">The scheduled message contract.</typeparam>
    /// <param name="scheduler">The scheduler that owns the message.</param>
    /// <param name="message">The scheduled message to cancel.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task CancelScheduledSendAsync<T>(this IMessageScheduler scheduler, ScheduledMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(message);

        return scheduler.CancelScheduledSendAsync(message, cancellationToken);
    }

    /// <summary>Cancels a scheduled message through the scheduler attached to a consume context.</summary>
    /// <typeparam name="T">The scheduled message contract.</typeparam>
    /// <param name="context">The consume context that provides the scheduler.</param>
    /// <param name="message">The scheduled message to cancel.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task CancelScheduledSendAsync<T>(this ConsumeContext context, ScheduledMessage<T> message, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(message);

        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.CancelScheduledSendAsync(message, cancellationToken);
    }
}
