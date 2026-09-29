using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Tracks scheduled messages so an in-memory outbox can cancel them when consumption fails.</summary>
internal sealed class InMemoryOutboxMessageSchedulerContext :
    MessageSchedulerContext,
    Advanced.IScheduleCancellationCapability
{
    readonly InMemoryOutboxDeferredMethodCollection _cancelMessages;
    readonly Task _clearToSend;
    readonly Uri _inputAddress;
    readonly object _listLock = new();
    readonly List<ScheduledMessage> _scheduledMessages;
    readonly Lazy<IMessageScheduler> _scheduler;

    /// <summary>Initializes scheduler tracking for one consume operation.</summary>
    /// <param name="consumeContext">The consume context that supplies the input address and scheduler scope.</param>
    /// <param name="schedulerFactory">The factory that resolves the message scheduler.</param>
    /// <param name="clearToSend">The task whose completion marks the outbox as committed.</param>
    public InMemoryOutboxMessageSchedulerContext(ConsumeContext consumeContext, MessageSchedulerFactory schedulerFactory, Task clearToSend)
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(schedulerFactory);
        ArgumentNullException.ThrowIfNull(clearToSend);

        _inputAddress = consumeContext.ReceiveContext.InputAddress;
        _clearToSend = clearToSend;

        SchedulerFactory = schedulerFactory;

        _scheduler = new Lazy<IMessageScheduler>(() =>
            schedulerFactory(consumeContext) ?? throw new InvalidOperationException("The message scheduler factory returned null."));

        _scheduledMessages = [];
        _cancelMessages = new InMemoryOutboxDeferredMethodCollection(clearToSend);
    }

    /// <summary>Gets the factory used to resolve the scoped message scheduler.</summary>
    public MessageSchedulerFactory SchedulerFactory { get; }

    /// <summary>Gets the clock exposed by the resolved message scheduler.</summary>
    public TimeProvider TimeProvider => _scheduler.Value.Advanced().TimeProvider;

    /// <inheritdoc />
    public Advanced.ScheduleCancellationMode CancellationMode =>
        (_scheduler.Value as Advanced.IScheduleCancellationCapability)?.CancellationMode
        ?? Advanced.ScheduleCancellationMode.Unknown;

    internal readonly record struct Checkpoint(int ScheduledMessageCount, int CancelMessageCount);

    internal Checkpoint CreateCheckpoint()
    {
        lock (_listLock)
            return new Checkpoint(_scheduledMessages.Count, _cancelMessages.CreateCheckpoint());
    }

    internal async Task DiscardSinceAsync(Checkpoint checkpoint)
    {
        ScheduledMessage[] scheduledMessages;
        lock (_listLock)
        {
            if (checkpoint.ScheduledMessageCount < 0 || checkpoint.ScheduledMessageCount > _scheduledMessages.Count)
                throw new ArgumentOutOfRangeException(nameof(checkpoint));

            int count = _scheduledMessages.Count - checkpoint.ScheduledMessageCount;
            scheduledMessages = count == 0
                ? []
                : _scheduledMessages.GetRange(checkpoint.ScheduledMessageCount, count).ToArray();
        }

        _cancelMessages.DiscardSince(checkpoint.CancelMessageCount);

        if (scheduledMessages.Length == 0)
            return;

        var tasks = new PendingTaskCollection(scheduledMessages.Length);
        foreach (var scheduledMessage in scheduledMessages)
            tasks.Add(CancelScheduledMessageAsync(scheduledMessage));

        await tasks.CompletedAsync().ConfigureAwait(false);

        async Task CancelScheduledMessageAsync(ScheduledMessage scheduledMessage)
        {
            await _scheduler.Value.Advanced().CancelScheduledSendAsync(scheduledMessage.Destination, scheduledMessage.TokenId).ConfigureAwait(false);

            lock (_listLock)
            {
                int index = _scheduledMessages.FindIndex(candidate => ReferenceEquals(candidate, scheduledMessage));
                if (index >= checkpoint.ScheduledMessageCount)
                    _scheduledMessages.RemoveAt(index);
            }
        }
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        var scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, messageType, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        var scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, messageType, pipe, cancellationToken)
            .ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(values);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync<T>(destinationAddress, dueAt, values, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync<T>(destinationAddress, dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, messageType, cancellationToken)
            .ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, messageType, pipe, cancellationToken)
            .ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync<T>(_inputAddress, dueAt, values, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync<T>(_inputAddress, dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ScheduledMessage<T> scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        var scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, messageType, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);
        var scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(pipe);
        var scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, messageType, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ScheduledMessage<T> scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync<T>(dueAt, values, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(pipe);
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().SchedulePublishAsync<T>(dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Defers cancellation of a scheduled publish until the outbox commits.</summary>
    /// <typeparam name="T">The scheduled message contract.</typeparam>
    /// <param name="tokenId">The scheduler token that identifies the publish.</param>
    /// <param name="cancellationToken">The token that cancels queuing or executing the cancellation.</param>
    /// <returns>A task that completes when the cancellation is queued or executed.</returns>
    public Task CancelScheduledPublishAsync<T>(Guid tokenId, CancellationToken cancellationToken)
        where T : class
    {
        if (tokenId == Guid.Empty)
            throw new ArgumentException("The scheduled-message token identifier must not be empty.", nameof(tokenId));

        return AddCancelMessageAsync(
            () => _scheduler.Value.Advanced().CancelScheduledPublishAsync<T>(tokenId, cancellationToken),
            cancellationToken);
    }

    /// <summary>Defers cancellation of a runtime-typed scheduled publish until the outbox commits.</summary>
    /// <param name="messageType">The scheduled message contract.</param>
    /// <param name="tokenId">The scheduler token that identifies the publish.</param>
    /// <param name="cancellationToken">The token that cancels queuing or executing the cancellation.</param>
    /// <returns>A task that completes when the cancellation is queued or executed.</returns>
    public Task CancelScheduledPublishAsync(Type messageType, Guid tokenId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        if (tokenId == Guid.Empty)
            throw new ArgumentException("The scheduled-message token identifier must not be empty.", nameof(tokenId));

        return AddCancelMessageAsync(
            () => _scheduler.Value.Advanced().CancelScheduledPublishAsync(messageType, tokenId, cancellationToken),
            cancellationToken);
    }

    /// <summary>Defers cancellation of a scheduled send until the outbox commits.</summary>
    /// <param name="destinationAddress">The destination of the scheduled send.</param>
    /// <param name="tokenId">The scheduler token that identifies the send.</param>
    /// <param name="cancellationToken">The token that cancels queuing or executing the cancellation.</param>
    /// <returns>A task that completes when the cancellation is queued or executed.</returns>
    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        if (tokenId == Guid.Empty)
            throw new ArgumentException("The scheduled-message token identifier must not be empty.", nameof(tokenId));

        return AddCancelMessageAsync(
            () => _scheduler.Value.Advanced().CancelScheduledSendAsync(destinationAddress, tokenId, cancellationToken),
            cancellationToken);
    }

    void AddScheduledMessage(ScheduledMessage scheduledMessage)
    {
        ArgumentNullException.ThrowIfNull(scheduledMessage);

        lock (_listLock)
        {
            if (!_clearToSend.IsCompleted)
                _scheduledMessages.Add(scheduledMessage);
        }
    }

    Task AddCancelMessageAsync(Func<Task> cancel, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cancel);
        try
        {
            lock (_listLock)
                if (_cancelMessages.TryQueue(cancel, cancellationToken))
                    return Task.CompletedTask;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        return cancel() ?? throw new InvalidOperationException("The scheduled-message cancellation returned a null task.");
    }

    /// <summary>Cancels every scheduled message tracked by this outbox.</summary>
    /// <param name="cancellationToken">The token that cancels the cleanup operation.</param>
    /// <returns>A task that completes when every tracked schedule is canceled.</returns>
    public Task CancelAllScheduledMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        ScheduledMessage[] scheduledMessages;

        lock (_listLock)
        {
            if (_scheduledMessages.Count == 0)
                return Task.CompletedTask;

            scheduledMessages = _scheduledMessages.ToArray();
        }

        var tasks = new PendingTaskCollection(scheduledMessages.Length);
        foreach (var scheduledMessage in scheduledMessages)
            tasks.Add(_scheduler.Value.Advanced().CancelScheduledSendAsync(scheduledMessage.Destination, scheduledMessage.TokenId, cancellationToken: cancellationToken));

        return tasks.CompletedAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Executes every deferred scheduled-message cancellation.</summary>
    /// <param name="cancellationToken">The token that cancels pending cancellation delivery.</param>
    /// <returns>A task that completes when all deferred cancellations finish.</returns>
    public Task ExecutePendingActionsAsync(CancellationToken cancellationToken = default)
    {
        return _cancelMessages.ExecuteAsync(true, cancellationToken: cancellationToken);
    }
}
