using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Schedules application messages for future delivery.</summary>
public interface IMessageScheduler
{
    /// <summary>Schedules a message for delivery to a destination.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a configured message for delivery to a destination.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a message for publication.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a previously scheduled send.</summary>
    /// <param name="scheduled">The scheduled used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CancelScheduledSendAsync(ScheduledMessage scheduled, CancellationToken cancellationToken = default);
}
