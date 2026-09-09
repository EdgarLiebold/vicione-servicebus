using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes scheduler pipes, runtime contract types, message initializers, and the scheduler clock.</summary>
public interface IAdvancedMessageScheduler :
    IMessageScheduler
{
    /// <inheritdoc />
    Task<ScheduledMessage<TMessage>> IMessageScheduler.ScheduleSendAsync<TMessage>(Uri destination, TimeSpan delay, TMessage message,
        CancellationToken cancellationToken)
    {
        ValidateDestination(destination);
        ArgumentNullException.ThrowIfNull(message);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ScheduledMessage<TMessage>>(cancellationToken);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(this, delay);
        return ScheduleSendAsync(destination, dueAt, message, cancellationToken);
    }

    /// <inheritdoc />
    Task<ScheduledMessage<TMessage>> IMessageScheduler.ScheduleSendAsync<TMessage>(Uri destination, TimeSpan delay, TMessage message,
        ScheduleOptions options, CancellationToken cancellationToken)
    {
        ValidateDestination(destination);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(options);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ScheduledMessage<TMessage>>(cancellationToken);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(this, delay);
        return ScheduleSendAsync(destination, dueAt, message, options, cancellationToken);
    }

    /// <inheritdoc />
    Task<ScheduledMessage<TMessage>> IMessageScheduler.SchedulePublishAsync<TMessage>(TimeSpan delay, TMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ScheduledMessage<TMessage>>(cancellationToken);

        DateTimeOffset dueAt = RelativeScheduleTime.GetDueAt(this, delay);
        return SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    /// <inheritdoc />
    Task<ScheduledMessage<T>> IMessageScheduler.ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, ScheduleOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ScheduleSendAsync(destination, dueAt, message, new ScheduleOptionsPipe<T>(options), cancellationToken);
    }

    /// <inheritdoc />
    Task IMessageScheduler.CancelScheduledSendAsync(ScheduledMessage scheduled, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scheduled);
        return CancelScheduledSendAsync(scheduled.Destination, scheduled.TokenId, cancellationToken);
    }

    /// <inheritdoc />
    new Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <inheritdoc />
    new Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a message with a typed send pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a message with an untyped send pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The untyped send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an object using its runtime contract type.</summary>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules an object using an explicit contract type.</summary>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract type under which the message will be delivered.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules an object with an untyped send pipe.</summary>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The untyped send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules an object using an explicit contract type and an untyped send pipe.</summary>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract type under which the message will be delivered.</param>
    /// <param name="pipe">The untyped send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>Initializes a message contract from values and schedules it for delivery.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes a message contract and schedules it with a typed send pipe.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes a message contract and schedules it with an untyped send pipe.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="destination">The address to which the message will be delivered.</param>
    /// <param name="dueAt">The time at which the message becomes eligible for delivery.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="pipe">The untyped send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled send at a destination.</summary>
    /// <param name="destination">The destination that owns the scheduled message.</param>
    /// <param name="tokenId">The token that identifies the scheduled message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduled send has been canceled.</returns>
    Task CancelScheduledSendAsync(Uri destination, Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>Schedules a publication with a typed send pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a publication with an untyped send pipe.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The untyped send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an object for publication using its runtime contract type.</summary>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken = default);

    /// <summary>Schedules an object for publication using an explicit contract type.</summary>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract type under which the message will be published.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules an object for publication with an untyped send pipe.</summary>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="pipe">The untyped send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules an object for publication using an explicit contract type and an untyped send pipe.</summary>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="messageType">The contract type under which the message will be published.</param>
    /// <param name="pipe">The untyped send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Initializes a message contract from values and schedules it for publication.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes a message contract and schedules it for publication with a typed send pipe.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="pipe">The typed send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes a message contract and schedules it for publication with an untyped send pipe.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="dueAt">The time at which the message becomes eligible for publication.</param>
    /// <param name="values">The property values used to initialize the message.</param>
    /// <param name="pipe">The untyped send pipeline applied before scheduling.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the accepted scheduled message.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled publication for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="tokenId">The token that identifies the scheduled publication.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduled publication has been canceled.</returns>
    Task CancelScheduledPublishAsync<T>(Guid tokenId, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled publication for a runtime contract type.</summary>
    /// <param name="messageType">The published message contract.</param>
    /// <param name="tokenId">The token that identifies the scheduled publication.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the scheduled publication has been canceled.</returns>
    Task CancelScheduledPublishAsync(Type messageType, Guid tokenId, CancellationToken cancellationToken = default);

    private static void ValidateDestination(Uri destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.IsAbsoluteUri)
            throw new ArgumentException("The scheduled destination must be an absolute URI.", nameof(destination));
    }
}
