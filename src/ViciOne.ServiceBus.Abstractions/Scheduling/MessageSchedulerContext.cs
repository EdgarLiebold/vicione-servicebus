using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides scheduling operations associated with a consume context.</summary>
public interface MessageSchedulerContext :
    IAdvancedMessageScheduler
{
    /// <summary>Gets the factory used to create the underlying scheduler for a consume context.</summary>
    MessageSchedulerFactory SchedulerFactory { get; }

    /// <summary>Schedules a message for delivery to the current receive endpoint.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a configured message for delivery to the current receive endpoint.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a configured message for delivery to the current receive endpoint.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an object for delivery to the current receive endpoint using its runtime contract type.</summary>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules an object for delivery to the current receive endpoint using an explicit contract
    /// type. The message must be assignable to that type.
    /// </summary>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType, CancellationToken cancellationToken = default);

    /// <summary>Schedules a configured object for delivery to the current receive endpoint.</summary>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a configured object for delivery to the current receive endpoint using an explicit
    /// contract type. The message must be assignable to that type.
    /// </summary>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it for delivery to the
    /// current receive endpoint.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it for delivery to the
    /// current receive endpoint with a typed send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it for delivery to the
    /// current receive endpoint with an untyped send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class;
}
