using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Adapts a message scheduler to the destination and lifetime of a consume context.</summary>
internal sealed class ConsumeMessageSchedulerContext :
    MessageSchedulerContext
{
    readonly Uri _inputAddress;
    readonly Lazy<IMessageScheduler> _scheduler;

    /// <summary>Creates a context-bound scheduler adapter.</summary>
    /// <param name="consumeContext">The consume context whose input address becomes the default destination.</param>
    /// <param name="schedulerFactory">The factory that creates the scheduler on first use.</param>
    public ConsumeMessageSchedulerContext(ConsumeContext consumeContext, MessageSchedulerFactory schedulerFactory)
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(schedulerFactory);

        _inputAddress = consumeContext.ReceiveContext.InputAddress;

        SchedulerFactory = schedulerFactory;

        _scheduler = new Lazy<IMessageScheduler>(() => schedulerFactory(consumeContext)
            ?? throw new InvalidOperationException("The message scheduler factory returned null."));
    }

    /// <inheritdoc />
    public MessageSchedulerFactory SchedulerFactory { get; }

    /// <inheritdoc />
    public TimeProvider TimeProvider => _scheduler.Value.Advanced().TimeProvider;

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync<T>(destinationAddress, dueAt, values, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync<T>(destinationAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync<T>(_inputAddress, dueAt, values, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, values, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().ScheduleSendAsync<T>(_inputAddress, dueAt, values, pipe, cancellationToken);
    }

    Task Advanced.IAdvancedMessageScheduler.CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().CancelScheduledSendAsync(destinationAddress, tokenId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, messageType, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, messageType, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync<T>(dueAt, values, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, values, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().SchedulePublishAsync<T>(dueAt, values, pipe, cancellationToken);
    }

    /// <inheritdoc />
    public Task CancelScheduledPublishAsync<T>(Guid tokenId, CancellationToken cancellationToken)
        where T : class
    {
        return _scheduler.Value.Advanced().CancelScheduledPublishAsync<T>(tokenId, cancellationToken);
    }

    /// <inheritdoc />
    public Task CancelScheduledPublishAsync(Type messageType, Guid tokenId, CancellationToken cancellationToken)
    {
        return _scheduler.Value.Advanced().CancelScheduledPublishAsync(messageType, tokenId, cancellationToken);
    }
}
