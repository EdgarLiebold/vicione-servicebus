using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes low-level scheduler pipes, runtime message types, initializers, and provider time.</summary>
public interface IAdvancedMessageScheduler :
    IMessageScheduler
{
    /// <inheritdoc />
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    new Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <inheritdoc />
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    new Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Gets the time source used for relative scheduling.</summary>
    TimeProvider TimeProvider { get; }

    /// <summary>Schedules a typed message through a typed send-context pipe.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a typed message through an untyped send-context pipe.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a runtime-typed message.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a message as the specified runtime type.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a runtime-typed message through a send-context pipe.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a message as the specified runtime type through a send-context pipe.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage> ScheduleSendAsync(Uri destination, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken = default);

    /// <summary>Schedules a message initialized from values.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an initialized message through a typed send-context pipe.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an initialized message through an untyped send-context pipe.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destination, DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled send by destination and token.</summary>
    /// <param name="destination">The destination used by the operation.</param>
    /// <param name="tokenId">The token id used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CancelScheduledSendAsync(Uri destination, Guid tokenId, CancellationToken cancellationToken = default);

    /// <summary>Schedules a typed publication through a typed send-context pipe.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a typed publication through an untyped send-context pipe.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules a runtime-typed publication.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken = default);

    /// <summary>Schedules a publication as the specified runtime type.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a runtime-typed publication through a send-context pipe.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a publication as the specified runtime type through a send-context pipe.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules a publication initialized from values.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an initialized publication through a typed send-context pipe.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Schedules an initialized publication through an untyped send-context pipe.</summary>
    /// <param name="dueAt">The due at used by the operation.</param>
    /// <param name="values">The values used by the operation.</param>
    /// <param name="pipe">The pipe used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled publication by message type and token.</summary>
    /// <param name="tokenId">The token id used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CancelScheduledPublishAsync<T>(Guid tokenId, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels a scheduled publication by runtime message type and token.</summary>
    /// <param name="messageType">The message type used by the operation.</param>
    /// <param name="tokenId">The token id used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CancelScheduledPublishAsync(Type messageType, Guid tokenId, CancellationToken cancellationToken = default);
}
