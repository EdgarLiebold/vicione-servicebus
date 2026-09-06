using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides extension methods for time span schedule publish.</summary>
public static class TimeSpanSchedulePublishExtensions
{
    /// <summary>Publish a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    /// <summary>Publish a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Publish a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, T message,
        Action<SendContext<T>> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publish a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, T message,
        Func<SendContext<T>, Task> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publish a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Publish a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, T message,
        Action<SendContext> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publish a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, T message,
        Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publishes an object as a message, using the type of the message instance.</summary>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        CancellationToken cancellationToken = default)
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    /// <summary>
    /// Publishes an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Publishes an object as a message.</summary>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Publishes an object as a message.</summary>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>Publishes an object as a message.</summary>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, callback.ToPipe(), cancellationToken);
    }

    /// <summary>
    /// Publishes an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>
    /// Publishes an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        Type messageType, Action<SendContext> callback, CancellationToken cancellationToken = default)
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, messageType, callback.ToPipe(), cancellationToken);
    }

    /// <summary>
    /// Publishes an object as a message, using the message type specified. If the object cannot be cast
    /// to the specified message type, an exception will be thrown.
    /// </summary>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The type of the message (use message.GetType() if desired).</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        Type messageType, Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, message, messageType, callback.ToPipe(), cancellationToken);
    }

    /// <summary>
    /// Publishes an interface message, initializing the properties of the interface using the anonymous
    /// object specified.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync<T>(dueAt, values, cancellationToken);
    }

    /// <summary>
    /// Publishes an interface message, initializing the properties of the interface using the anonymous
    /// object specified.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Publishes an interface message, initializing the properties of the interface using the anonymous
    /// object specified.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, object values,
        Action<SendContext<T>> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, values, callback.ToPipe(), cancellationToken);
    }

    /// <summary>
    /// Publishes an interface message, initializing the properties of the interface using the anonymous
    /// object specified.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, object values,
        Func<SendContext<T>, Task> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync(dueAt, values, callback.ToPipe(), cancellationToken);
    }

    /// <summary>
    /// Publishes an interface message, initializing the properties of the interface using the anonymous
    /// object specified.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync<T>(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>
    /// Publishes an interface message, initializing the properties of the interface using the anonymous
    /// object specified.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, object values,
        Action<SendContext> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync<T>(dueAt, values, callback.ToPipe(), cancellationToken);
    }

    /// <summary>
    /// Publishes an interface message, initializing the properties of the interface using the anonymous
    /// object specified.
    /// </summary>
    /// <typeparam name="T">The interface type to send.</typeparam>
    /// <param name="scheduler">The message scheduler.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The property values to initialize on the interface.</param>
    /// <param name="callback">The send callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the publish operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, TimeSpan delay, object values,
        Func<SendContext, Task> callback, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = scheduler.Advanced().TimeProvider.GetUtcNow() + delay;

        return scheduler.Advanced().SchedulePublishAsync<T>(dueAt, values, callback.ToPipe(), cancellationToken);
    }
}
