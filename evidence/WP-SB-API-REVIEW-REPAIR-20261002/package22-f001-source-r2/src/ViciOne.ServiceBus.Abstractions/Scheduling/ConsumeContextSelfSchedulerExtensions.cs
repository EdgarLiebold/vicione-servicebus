using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides advanced scheduling overloads targeting the current receive endpoint.</summary>
public static class ConsumeContextSelfSchedulerExtensions
{
    /// <summary>Schedules a message for delivery to the current receive endpoint.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(context.ReceiveContext.InputAddress, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules a message to the current receive endpoint with a typed send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(context.ReceiveContext.InputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a message to the current receive endpoint with an untyped send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(context.ReceiveContext.InputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules an object to the current receive endpoint using its runtime contract type.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken = default)
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(context.ReceiveContext.InputAddress, dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Schedules an object to the current receive endpoint using an explicit contract type. The
    /// message must be assignable to that type.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, DateTimeOffset dueAt, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(context.ReceiveContext.InputAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules an object to the current receive endpoint with an untyped send pipe.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, DateTimeOffset dueAt, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(context.ReceiveContext.InputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Schedules an object to the current receive endpoint using an explicit contract type and an
    /// untyped send pipe. The message must be assignable to the specified type.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, DateTimeOffset dueAt, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(context.ReceiveContext.InputAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it to the current
    /// receive endpoint.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync<T>(context.ReceiveContext.InputAddress, dueAt, values, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it to the current
    /// receive endpoint with a typed send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync(context.ReceiveContext.InputAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it to the current
    /// receive endpoint with an untyped send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = GetScheduler(context);

        return scheduler.ScheduleSendAsync<T>(context.ReceiveContext.InputAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules a message to the current receive endpoint after a relative delay.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, TimeSpan delay, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules a delayed message to the current receive endpoint with a typed send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, TimeSpan delay, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a delayed message to the current receive endpoint with an untyped send pipe.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, TimeSpan delay, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a delayed object using its runtime contract type.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, TimeSpan delay, object message,
        CancellationToken cancellationToken = default)
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Schedules a delayed object using an explicit contract type. The message must be assignable to
    /// that type.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, TimeSpan delay, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a delayed object with an untyped send pipe.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, TimeSpan delay, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it to the current
    /// receive endpoint after a relative delay.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, TimeSpan delay, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync<T>(context, dueAt, values, cancellationToken);
    }

    /// <summary>
    /// Schedules a delayed object using an explicit contract type and an untyped send pipe. The
    /// message must be assignable to that type.
    /// </summary>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, TimeSpan delay, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it after a delay with a
    /// typed send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, TimeSpan delay, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync(context, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Initializes a message contract from the supplied values and schedules it after a delay with an
    /// untyped send pipe.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, TimeSpan delay, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = GetDueAt(context, delay);

        return ScheduleSendAsync<T>(context, dueAt, values, pipe, cancellationToken);
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
