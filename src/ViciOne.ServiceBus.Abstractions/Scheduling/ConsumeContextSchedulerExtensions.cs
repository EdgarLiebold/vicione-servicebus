using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides advanced scheduling overloads for consume contexts.</summary>
public static class ConsumeContextSchedulerExtensions
{
    /// <summary>Schedules a message for delivery to a destination.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules a configured message with a typed send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a configured message with an untyped send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules an object using its runtime contract type.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken = default)
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Schedules an object using an explicit contract type. The message must be assignable to that
    /// type.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules an object with an untyped send pipe.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules an object using an explicit contract type and an untyped send pipe. The message must
    /// be assignable to that type.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it for delivery.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync<T>(destinationAddress, dueAt, values, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it with a typed send
    /// pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it with an untyped send
    /// pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync<T>(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules a message for delivery after a relative delay.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules a delayed message with a typed send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a delayed message with an untyped send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a delayed object using its runtime contract type.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object message,
        CancellationToken cancellationToken = default)
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Schedules a delayed object using an explicit contract type. The message must be assignable to
    /// that type.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a delayed object with an untyped send pipe.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it after a relative
    /// delay.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync<T>(context, destinationAddress, dueAt, values, cancellationToken);
    }

    /// <summary>
    /// Schedules a delayed object using an explicit contract type and an untyped send pipe. The
    /// message must be assignable to that type.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it after a delay with a
    /// typed send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it after a delay with an
    /// untyped send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="destinationAddress">The address to which the scheduled message will be delivered.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync<T>(context, destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    static DateTimeOffset GetDueAt(ConsumeContext context, TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetTimeProvider().GetUtcNow() + delay;
    }

    static MessageSchedulerContext GetScheduler(ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetPayload<MessageSchedulerContext>();
    }
}
