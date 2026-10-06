using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides relative-delay scheduling for a consume-bound message scheduler context.</summary>
public static class RelativeMessageSchedulerContextExtensions
{
    /// <summary>Schedules a typed message to the current receive endpoint after a delay.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this MessageSchedulerContext scheduler, TimeSpan delay,
        TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(message);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync(dueAt, message, cancellationToken);
    }

    /// <summary>Schedules a typed message to the current receive endpoint after a delay through a typed send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this MessageSchedulerContext scheduler, TimeSpan delay,
        TMessage message, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed message to the current receive endpoint after a delay through an untyped send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this MessageSchedulerContext scheduler, TimeSpan delay,
        TMessage message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules an object to the current receive endpoint after a delay using its runtime contract.</summary>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this MessageSchedulerContext scheduler, TimeSpan delay, object message,
        CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(message);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync(dueAt, message, cancellationToken);
    }

    /// <summary>Schedules an object to the current receive endpoint after a delay using an explicit contract.</summary>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract under which the message will be delivered.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this MessageSchedulerContext scheduler, TimeSpan delay, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ValidateMessage(message, messageType);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync(dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed object to the current receive endpoint after a delay through a send-context pipe.</summary>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this MessageSchedulerContext scheduler, TimeSpan delay, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules an explicitly typed object to the current receive endpoint after a delay through a send-context pipe.</summary>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract under which the message will be delivered.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this MessageSchedulerContext scheduler, TimeSpan delay, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ValidateMessage(message, messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync(dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Initializes and schedules a message contract to the current receive endpoint after a delay.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this MessageSchedulerContext scheduler, TimeSpan delay,
        object values, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(values);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync<TMessage>(dueAt, values, cancellationToken);
    }

    /// <summary>Initializes and schedules a message contract after a delay through a typed send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this MessageSchedulerContext scheduler, TimeSpan delay,
        object values, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync<TMessage>(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Initializes and schedules a message contract after a delay through an untyped send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The consume-bound scheduler.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this MessageSchedulerContext scheduler, TimeSpan delay,
        object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.ScheduleSendAsync<TMessage>(dueAt, values, pipe, cancellationToken);
    }

    static void ValidateScheduler(MessageSchedulerContext scheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
    }

    static void ValidateMessage(object message, Type messageType)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        if (messageType.IsValueType || messageType.IsByRef || messageType.IsPointer || messageType.ContainsGenericParameters)
            throw new ArgumentException("The message contract must be a closed reference type.", nameof(messageType));
        if (!messageType.IsInstanceOfType(message))
            throw new ArgumentException($"The message is not assignable to {TypeCache.GetShortName(messageType)}.", nameof(message));
    }
}
