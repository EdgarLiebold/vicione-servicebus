using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Schedules application messages for future delivery.</summary>
public interface IMessageScheduler
{
    /// <summary>Schedules a message for delivery to a destination.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a configured message for delivery to a destination.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="options">The headers and transport-independent settings applied to the scheduled send.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a message for publication.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a previously scheduled send.</summary>
    /// <param name="scheduled">The scheduled message to cancel.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledSendAsync(ScheduledMessage scheduled, CancellationToken cancellationToken = default);
}
