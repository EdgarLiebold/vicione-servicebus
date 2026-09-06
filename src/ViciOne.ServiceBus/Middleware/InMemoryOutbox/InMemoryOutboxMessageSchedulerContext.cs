using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Carries state for in memory outbox message scheduler operations.</summary>
public class InMemoryOutboxMessageSchedulerContext :
    MessageSchedulerContext
{
    readonly InMemoryOutboxDeferredMethodCollection _cancelMessages;
    readonly Task _clearToSend;
    readonly Uri _inputAddress;
    readonly object _listLock = new object();
    readonly List<ScheduledMessage> _scheduledMessages;
    readonly Lazy<IMessageScheduler> _scheduler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="schedulerFactory">The scheduler factory.</param>
    /// <param name="clearToSend">The clear to send.</param>
    public InMemoryOutboxMessageSchedulerContext(ConsumeContext consumeContext, MessageSchedulerFactory schedulerFactory, Task clearToSend)
    {
        _inputAddress = consumeContext.ReceiveContext.InputAddress;
        _clearToSend = clearToSend;

        SchedulerFactory = schedulerFactory;

        _scheduler = new Lazy<IMessageScheduler>(() => schedulerFactory(consumeContext));

        _scheduledMessages = [];
        _cancelMessages = new InMemoryOutboxDeferredMethodCollection();
    }

    /// <summary>Gets the scheduler factory.</summary>
    public MessageSchedulerFactory SchedulerFactory { get; }

    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider => _scheduler.Value.Advanced().TimeProvider;

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

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message,
        CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        var scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, messageType, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        var scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage> ScheduleSendAsync(Uri destinationAddress, DateTimeOffset dueAt, object message, Type messageType,
        IPipe<SendContext> pipe, CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, message, messageType, pipe, cancellationToken)
            .ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync<T>(destinationAddress, dueAt, values, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(destinationAddress, dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, object values,
        IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync<T>(destinationAddress, dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, messageType, cancellationToken)
            .ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage> ScheduleSendAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, message, messageType, pipe, cancellationToken)
            .ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync<T>(_inputAddress, dueAt, values, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync(_inputAddress, dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().ScheduleSendAsync<T>(_inputAddress, dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, T message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType,
        CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, messageType, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage> SchedulePublishAsync(DateTimeOffset dueAt, object message, Type messageType, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
    {
        var scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, message, messageType, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync<T>(dueAt, values, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage = await _scheduler.Value.Advanced().SchedulePublishAsync(dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Schedules publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dueAt">The due at.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule publish outcome.</returns>
    public async Task<ScheduledMessage<T>> SchedulePublishAsync<T>(DateTimeOffset dueAt, object values, IPipe<SendContext> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ScheduledMessage<T> scheduledMessage =
            await _scheduler.Value.Advanced().SchedulePublishAsync<T>(dueAt, values, pipe, cancellationToken).ConfigureAwait(false);

        AddScheduledMessage(scheduledMessage);

        return scheduledMessage;
    }

    /// <summary>Determines whether the current value can cel scheduled publish.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledPublishAsync<T>(Guid tokenId, CancellationToken cancellationToken)
        where T : class
    {
        return AddCancelMessageAsync(() => _scheduler.Value.Advanced().CancelScheduledPublishAsync<T>(tokenId, cancellationToken));
    }

    /// <summary>Determines whether the current value can cel scheduled publish.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledPublishAsync(Type messageType, Guid tokenId, CancellationToken cancellationToken)
    {
        return AddCancelMessageAsync(() => _scheduler.Value.Advanced().CancelScheduledPublishAsync(messageType, tokenId, cancellationToken));
    }

    /// <summary>Determines whether the current value can cel scheduled send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        return AddCancelMessageAsync(() => _scheduler.Value.Advanced().CancelScheduledSendAsync(destinationAddress, tokenId, cancellationToken));
    }

    void AddScheduledMessage(ScheduledMessage scheduledMessage)
    {
        if (_clearToSend.IsCompleted)
            return;

        lock (_listLock)
            _scheduledMessages.Add(scheduledMessage);
    }

    Task AddCancelMessageAsync(Func<Task> cancel)
    {
        if (_clearToSend.IsCompleted)
            return cancel();

        lock (_listLock)
            _cancelMessages.AddAsync(cancel);

        return Task.CompletedTask;
    }

    /// <summary>Determines whether the current value can cel all scheduled messages.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelAllScheduledMessagesAsync(CancellationToken cancellationToken = default)
    {
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

    /// <summary>Executes pending actions.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecutePendingActionsAsync(CancellationToken cancellationToken = default)
    {
        return _cancelMessages.ExecuteAsync(true, cancellationToken: cancellationToken);
    }
}
