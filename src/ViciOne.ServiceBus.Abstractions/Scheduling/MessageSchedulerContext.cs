using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for message scheduler context.
/// </summary>
public interface MessageSchedulerContext :
    Advanced.IAdvancedMessageScheduler
{
    /// <summary>
    /// Gets the scheduler factory value.
    /// </summary>
    MessageSchedulerFactory SchedulerFactory { get; }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sends an object as a message, using the type of the message instance.
    /// </summary>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an object as a message.
    /// </summary>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired)</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class;
}
