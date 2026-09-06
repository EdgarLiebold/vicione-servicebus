using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes low-level scheduler pipes, runtime message types, initializers, and provider time.</summary>
public interface IAdvancedMessageScheduler :
    IMessageScheduler
{
    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destination">The destination.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> IMessageScheduler.ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, ScheduleOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ScheduleSendAsync(destination, dueAt, message, new ScheduleOptionsPipe<T>(options), cancellationToken);
    }

    /// <summary>Determines whether the current value can cel scheduled send.</summary>
    /// <param name="scheduled">The scheduled.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task IMessageScheduler.CancelScheduledSendAsync(ScheduledMessage scheduled, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scheduled);
        return CancelScheduledSendAsync(scheduled.Destination, scheduled.TokenId, cancellationToken);
    }

    /// <inheritdoc />
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to schedule.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    new Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <inheritdoc />
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to schedule.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    new Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Gets the time provider.</summary>
    TimeProvider TimeProvider { get; }

    /// <summary>Schedules a typed message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a typed message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a runtime-typed message.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a message as the specified runtime type.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a runtime-typed message through a send-context pipe.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a message as the specified runtime type through a send-context pipe.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>Schedules a message initialized from values.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an initialized message through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an initialized message through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled send by destination and token.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="tokenId">The token id used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledSendAsync(Uri destination, Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>Schedules a typed publication through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a typed publication through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a runtime-typed publication.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken = default);

    /// <summary>Schedules a publication as the specified runtime type.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a runtime-typed publication through a send-context pipe.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a publication as the specified runtime type through a send-context pipe.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a publication initialized from values.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an initialized publication through a typed send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an initialized publication through an untyped send-context pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled publication by message type and token.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="tokenId">The token id used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledPublishAsync<T>(Guid tokenId, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled publication by runtime message type and token.</summary>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="tokenId">The token id used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CancelScheduledPublishAsync(Type messageType, Guid tokenId, CancellationToken cancellationToken = default);
}
