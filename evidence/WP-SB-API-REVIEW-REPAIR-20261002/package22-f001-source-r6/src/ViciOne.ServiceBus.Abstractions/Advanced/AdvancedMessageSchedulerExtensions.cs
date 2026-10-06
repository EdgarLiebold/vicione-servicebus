namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides access to low-level scheduling operations.</summary>
public static class AdvancedMessageSchedulerExtensions
{
    /// <summary>Returns the advanced scheduling contract implemented by the scheduler.</summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <returns>The scheduler's advanced contract.</returns>
    public static IAdvancedMessageScheduler Advanced(this IMessageScheduler scheduler)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        return scheduler as IAdvancedMessageScheduler
            ?? throw new NotSupportedException($"The message scheduler '{scheduler.GetType().FullName}' does not expose advanced operations.");
    }

    /// <summary>Schedules a typed message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.ScheduleSendAsync(destination, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> ScheduleSendAsync<T>(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        T message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.ScheduleSendAsync(destination, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a message as the specified runtime type.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract type under which the message will be delivered.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object message, Type messageType, CancellationToken cancellationToken = default)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return advanced.ScheduleSendAsync(destination, dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed message through a send-context pipe.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object message, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.ScheduleSendAsync(destination, dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a message as the specified runtime type through a send-context pipe.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract type under which the message will be delivered.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> ScheduleSendAsync(this IMessageScheduler scheduler, Uri destination, DateTimeOffset dueAt,
        object message, Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.ScheduleSendAsync(destination, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Cancels a scheduled send by destination and token.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="destination">The destination that owns the scheduled message.</param>
    /// <param name="tokenId">The token that identifies the scheduled message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the provider-specific cancellation operation. Completion may mean local command capture; whether cancellation removes the scheduled message depends on the provider capability and subsequent dispatch.</returns>
    public static Task CancelScheduledSendAsync(this IMessageScheduler scheduler, Uri destination, Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(destination);
        if (tokenId == Guid.Empty)
            throw new ArgumentException("The scheduled-message token cannot be empty.", nameof(tokenId));

        return advanced.CancelScheduledSendAsync(destination, tokenId, cancellationToken);
    }

    /// <summary>Schedules a typed publication through a typed send-context pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, DateTimeOffset dueAt, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a typed publication through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage<T>> SchedulePublishAsync<T>(this IMessageScheduler scheduler, DateTimeOffset dueAt, T message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a publication as the specified runtime type.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract type under which the message will be published.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, DateTimeOffset dueAt, object message,
        Type messageType, CancellationToken cancellationToken = default)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);

        return advanced.SchedulePublishAsync(dueAt, message, messageType, cancellationToken);
    }

    /// <summary>Schedules a runtime-typed publication through a send-context pipe.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, DateTimeOffset dueAt, object message,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <summary>Schedules a publication as the specified runtime type through a send-context pipe.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract type under which the message will be published.</param>
    /// <param name="pipe">The send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the configured scheduling result. Completion may mean local capture rather than transport acceptance and does not confirm future delivery or consumption.</returns>
    public static Task<ScheduledMessage> SchedulePublishAsync(this IMessageScheduler scheduler, DateTimeOffset dueAt, object message,
        Type messageType, IPipe<SendContext> pipe, CancellationToken cancellationToken = default)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);

        return advanced.SchedulePublishAsync(dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <summary>Cancels a scheduled publication by message type and token.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="tokenId">The token that identifies the scheduled publication.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the provider-specific cancellation operation. Completion may mean local command capture; whether cancellation removes the scheduled publication depends on the provider capability and subsequent dispatch.</returns>
    public static Task CancelScheduledPublishAsync<T>(this IMessageScheduler scheduler, Guid tokenId,
        CancellationToken cancellationToken = default)
        where T : class
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        if (tokenId == Guid.Empty)
            throw new ArgumentException("The scheduled-publication token cannot be empty.", nameof(tokenId));

        return advanced.CancelScheduledPublishAsync<T>(tokenId, cancellationToken);
    }

    /// <summary>Cancels a scheduled publication by runtime message type and token.</summary>
    /// <param name="scheduler">The scheduler that performs the operation.</param>
    /// <param name="messageType">The published message contract.</param>
    /// <param name="tokenId">The token that identifies the scheduled publication.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes the provider-specific cancellation operation. Completion may mean local command capture; whether cancellation removes the scheduled publication depends on the provider capability and subsequent dispatch.</returns>
    public static Task CancelScheduledPublishAsync(this IMessageScheduler scheduler, Type messageType, Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        IAdvancedMessageScheduler advanced = scheduler.Advanced();
        ArgumentNullException.ThrowIfNull(messageType);
        if (tokenId == Guid.Empty)
            throw new ArgumentException("The scheduled-publication token cannot be empty.", nameof(tokenId));

        return advanced.CancelScheduledPublishAsync(messageType, tokenId, cancellationToken);
    }
}
