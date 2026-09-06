using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Schedules publications from the time and scheduler available on a consume context.</summary>
public static class SchedulePublishExtensions
{
    /// <summary>Schedules a message for publication at an absolute time.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    /// <summary>Schedules a message for publication through a typed send pipeline at an absolute time.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed message for publication through an untyped send pipeline at an absolute time.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a message for publication using its runtime type as the contract.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this ConsumeContext context, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var messageType = message.GetType();

        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync(dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a message for publication as an explicitly selected contract.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The message contract exposed to consumers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this ConsumeContext context, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken = default)
    {
        if (messageType == null)
            throw new ArgumentNullException(nameof(messageType));

        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync(dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed message for publication through a send pipeline.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this ConsumeContext context, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a message as an explicit contract for publication through a send pipeline.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The message contract exposed to consumers.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this ConsumeContext context, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync(dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Initializes a message contract from property values and schedules it for publication at an absolute time.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));


        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync<T>(dueAt, values, cancellationToken);
    }

    /// <summary>Initializes a message contract and schedules it through a typed send pipeline at an absolute time.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Initializes a message contract and schedules it through an untyped send pipeline at an absolute time.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="dueAt">The date and time at which the scheduler should make the message eligible for delivery.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var scheduler = context.GetPayload<MessageSchedulerContext>();

        return scheduler.SchedulePublishAsync<T>(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules a message for publication after a relative delay.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, TimeSpan delay, T message,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync(context, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules a message through a typed send pipeline after a relative delay.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, TimeSpan delay, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync(context, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed message through an untyped send pipeline after a relative delay.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, TimeSpan delay, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync(context, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a message after a delay using its runtime type as the contract.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this ConsumeContext context, TimeSpan delay, object message,
        CancellationToken cancellationToken = default)
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync(context, dueAt, message, cancellationToken);
    }

    /// <summary>Schedules a message after a delay as an explicitly selected contract.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The message contract exposed to consumers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this ConsumeContext context, TimeSpan delay, object message, Type messageType,
        CancellationToken cancellationToken = default)
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync(context, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed message through a send pipeline after a delay.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this ConsumeContext context, TimeSpan delay, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync(context, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a message as an explicit contract through a send pipeline after a delay.</summary>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="message">The message object.</param>
    /// <param name="messageType">The message contract exposed to consumers.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this ConsumeContext context, TimeSpan delay, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync(context, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Initializes a message contract from property values and schedules it after a delay.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, TimeSpan delay, object values,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync<T>(context, dueAt, values, cancellationToken);
    }

    /// <summary>Initializes a message contract and schedules it through a typed send pipeline after a delay.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, TimeSpan delay, object values,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync(context, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Initializes a message contract and schedules it through an untyped send pipeline after a delay.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="context">The consume context.</param>
    /// <param name="delay">The relative delay before the scheduler makes the message eligible for delivery.</param>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this ConsumeContext context, TimeSpan delay, object values,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        var dueAt = context.GetTimeProvider().GetUtcNow() + delay;

        return SchedulePublishAsync<T>(context, dueAt, values, pipe, cancellationToken);
    }
}
