using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Schedules application messages for future delivery.</summary>
public interface IMessageScheduler
{
    /// <summary>Schedules a message for delivery to a destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a configured message for delivery to a destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a message for publication.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a previously scheduled send.</summary>
    /// <param name="scheduled">The scheduled used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledSendAsync(ScheduledMessage scheduled, CancellationToken cancellationToken = default);
}
