using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Schedules application messages for future delivery.</summary>
public interface IMessageScheduler
{
    /// <summary>Gets the clock used to calculate relative due times.</summary>
    TimeProvider TimeProvider { get; }

    /// <summary>Schedules a message for delivery to a destination.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, DateTimeOffset dueAt, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Schedules a configured message for delivery to a destination.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="options">The headers and transport-independent settings applied to the scheduled send.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, DateTimeOffset dueAt, TMessage message, ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Schedules a message for delivery after a relative delay.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="destination">The absolute address to which the message will be delivered.</param>
    /// <param name="delay">The nonnegative delay before the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the accepted scheduled message.</returns>
    Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, TimeSpan delay, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Schedules a configured message for delivery after a relative delay.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="destination">The absolute address to which the message will be delivered.</param>
    /// <param name="delay">The nonnegative delay before the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="options">The headers and transport-independent settings applied to the scheduled send.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the accepted scheduled message.</returns>
    Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(Uri destination, TimeSpan delay, TMessage message, ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Schedules a message for publication.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(DateTimeOffset dueAt, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Schedules a message for publication after a relative delay.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="delay">The nonnegative delay before the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the accepted scheduled publication.</returns>
    Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(TimeSpan delay, TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Cancels a previously scheduled send.</summary>
    /// <param name="scheduled">The scheduled message to cancel.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledSendAsync(ScheduledMessage scheduled, CancellationToken cancellationToken = default);
}
