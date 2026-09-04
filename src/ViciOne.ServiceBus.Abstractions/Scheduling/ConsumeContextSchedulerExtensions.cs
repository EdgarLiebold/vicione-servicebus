using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

public static class ConsumeContextSchedulerExtensions
{
    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends an object as a message, using the type of the message instance.
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken = default)
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Sends an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Sends an object as a message.
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired)</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync<T>(destinationAddress, dueAt, values, cancellationToken);
    }

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="dueAt">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, DateTimeOffset dueAt, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.ScheduleSendAsync<T>(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends an object as a message, using the type of the message instance.
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object message,
        CancellationToken cancellationToken = default)
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Sends an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired)</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>
    /// Sends an object as a message.
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync<T>(context, destinationAddress, dueAt, values, cancellationToken);
    }

    /// <summary>
    /// Sends an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="context">The consume context</param>
    /// <param name="message">The message object</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired)</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync(context, destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync(context, destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends an interface message, initializing the properties of the interface using the anonymous
    /// object specified
    /// </summary>
    /// <typeparam name="T">The interface type to send</typeparam>
    /// <param name="context">The consume context</param>
    /// <param name="values">The property values to initialize on the interface</param>
    /// <param name="destinationAddress">The destination address where the schedule message should be sent</param>
    /// <param name="delay">The time at which the message should be delivered to the queue</param>
    /// <param name="pipe"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this ConsumeContext context, Uri destinationAddress, TimeSpan delay, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return ScheduleSendAsync<T>(context, destinationAddress, dueAt, values, pipe, cancellationToken);
    }
}
