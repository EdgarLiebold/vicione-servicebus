using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides relative-delay overloads for advanced scheduling operations.</summary>
public static class AdvancedRelativeMessageSchedulerExtensions
{
    /// <summary>Schedules a typed message after a delay through a typed send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The absolute delivery address.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this IMessageScheduler scheduler, Uri destination,
        TimeSpan delay, TMessage message, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ValidateDestination(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.ScheduleSendAsync(destination, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed message after a delay through an untyped send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The absolute delivery address.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this IMessageScheduler scheduler, Uri destination,
        TimeSpan delay, TMessage message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ValidateDestination(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.ScheduleSendAsync(destination, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules an object after a delay using an explicit message contract.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The absolute delivery address.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract under which the message will be delivered.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this IMessageScheduler scheduler, Uri destination, TimeSpan delay,
        object message, Type messageType, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ValidateDestination(destination);
        ValidateMessage(message, messageType);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.ScheduleSendAsync(destination, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed object after a delay through a send-context pipe.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The absolute delivery address.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this IMessageScheduler scheduler, Uri destination, TimeSpan delay,
        object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ValidateDestination(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.ScheduleSendAsync(destination, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules an explicitly typed object after a delay through a send-context pipe.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The absolute delivery address.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract under which the message will be delivered.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this IMessageScheduler scheduler, Uri destination, TimeSpan delay,
        object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ValidateDestination(destination);
        ValidateMessage(message, messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.ScheduleSendAsync(destination, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Initializes and schedules a message contract after a delay.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The absolute delivery address.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this IMessageScheduler scheduler, Uri destination,
        TimeSpan delay, object values, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ValidateDestination(destination);
        ArgumentNullException.ThrowIfNull(values);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.ScheduleSendAsync<TMessage>(destination, dueAt, values, cancellationToken);
    }

    /// <summary>Initializes and schedules a message contract after a delay through a typed send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The absolute delivery address.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this IMessageScheduler scheduler, Uri destination,
        TimeSpan delay, object values, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ValidateDestination(destination);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.ScheduleSendAsync<TMessage>(destination, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Initializes and schedules a message contract after a delay through an untyped send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The absolute delivery address.</param>
    /// <param name="delay">The nonnegative delay before delivery becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> ScheduleSendAsync<TMessage>(this IMessageScheduler scheduler, Uri destination,
        TimeSpan delay, object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ValidateDestination(destination);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.ScheduleSendAsync<TMessage>(destination, dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed publication after a delay through a typed send-context pipe.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="delay">The nonnegative delay before publication becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(this IMessageScheduler scheduler, TimeSpan delay,
        TMessage message, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed publication after a delay through an untyped send-context pipe.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="delay">The nonnegative delay before publication becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(this IMessageScheduler scheduler, TimeSpan delay,
        TMessage message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules an object for publication after a delay using an explicit message contract.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="delay">The nonnegative delay before publication becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract under which the message will be published.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ValidateMessage(message, messageType);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.SchedulePublishAsync(dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed object for publication after a delay through a send-context pipe.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="delay">The nonnegative delay before publication becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules an explicitly typed object for publication after a delay through a send-context pipe.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="delay">The nonnegative delay before publication becomes eligible.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract under which the message will be published.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, TimeSpan delay, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        ValidateScheduler(scheduler);
        ValidateMessage(message, messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.SchedulePublishAsync(dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Initializes and schedules a message contract for publication after a delay.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="delay">The nonnegative delay before publication becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(this IMessageScheduler scheduler, TimeSpan delay,
        object values, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(values);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.SchedulePublishAsync<TMessage>(dueAt, values, cancellationToken);
    }

    /// <summary>Initializes and schedules a publication after a delay through a typed send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="delay">The nonnegative delay before publication becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(this IMessageScheduler scheduler, TimeSpan delay,
        object values, IPipe<SendContext<TMessage>> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.SchedulePublishAsync<TMessage>(dueAt, values, pipe, cancellationToken);
    }

    /// <summary>Initializes and schedules a publication after a delay through an untyped send-context pipe.</summary>
    /// <typeparam name="TMessage">The message contract to initialize.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="delay">The nonnegative delay before publication becomes eligible.</param>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<TMessage>> SchedulePublishAsync<TMessage>(this IMessageScheduler scheduler, TimeSpan delay,
        object values, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ValidateScheduler(scheduler);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);

        IAdvancedMessageScheduler advanced = Prepare(scheduler, delay, out DateTimeOffset dueAt);
        return advanced.SchedulePublishAsync<TMessage>(dueAt, values, pipe, cancellationToken);
    }

    static IAdvancedMessageScheduler Prepare(IMessageScheduler scheduler, TimeSpan delay, out DateTimeOffset dueAt)
    {
        dueAt = RelativeScheduleTime.GetDueAt(scheduler, delay);
        return scheduler.Advanced();
    }

    static void ValidateScheduler(IMessageScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
    }

    static void ValidateDestination(Uri destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.IsAbsoluteUri)
            throw new ArgumentException("The scheduled destination must be an absolute URI.", nameof(destination));
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
