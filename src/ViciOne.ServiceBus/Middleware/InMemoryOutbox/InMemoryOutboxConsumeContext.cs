using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Batching.Contexts;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Buffers outgoing operations until the enclosing consume pipeline commits or discards them.</summary>
public class InMemoryOutboxConsumeContext :
    ConsumeContextProxy,
    OutboxContext
{
    readonly TaskCompletionSource<InMemoryOutboxConsumeContext> _clearToSend;
    readonly InMemoryOutboxDeferredMethodCollection _deferredMethods;
    readonly InMemoryOutboxMessageSchedulerContext? _outboxSchedulerContext;

    /// <summary>Initializes an in-memory outbox over an existing consume context.</summary>
    /// <param name="context">The consume context whose outgoing operations are deferred.</param>
    protected InMemoryOutboxConsumeContext(ConsumeContext context)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        CapturedContext = context;

        var outboxReceiveContext = new InMemoryOutboxReceiveContext(this, context.ReceiveContext);

        ReceiveContext = outboxReceiveContext;
        SetPublishEndpointProvider(outboxReceiveContext.PublishEndpointProvider);

        _clearToSend = TaskCompletionSources.Create<InMemoryOutboxConsumeContext>();

        _deferredMethods = new InMemoryOutboxDeferredMethodCollection(_clearToSend.Task);

        if (context.TryGetPayload(out MessageSchedulerContext? schedulerContext))
        {
            _outboxSchedulerContext = (InMemoryOutboxMessageSchedulerContext)context.AddOrUpdatePayload<MessageSchedulerContext>(
                () => new InMemoryOutboxMessageSchedulerContext(context, schedulerContext.SchedulerFactory, _clearToSend.Task),
                existing => new InMemoryOutboxMessageSchedulerContext(context, existing.SchedulerFactory, _clearToSend.Task));
        }
    }

    /// <summary>Gets the original consume context outside the outbox decorator.</summary>
    public ConsumeContext CapturedContext { get; }

    /// <summary>Gets a task that completes when deferred operations may be delivered.</summary>
    public Task ClearToSend => _clearToSend.Task;

    /// <summary>Queues an asynchronous operation until the outbox is committed.</summary>
    /// <param name="method">The operation to execute after the outbox is cleared for delivery.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the operation has been queued or invoked.</returns>
    public Task AddAsync(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        return _deferredMethods.AddAsync(method, cancellationToken: cancellationToken);
    }

    /// <summary>Captures the current deferred-operation and scheduler positions.</summary>
    /// <returns>A checkpoint that can be used to discard later additions.</returns>
    public virtual OutboxCheckpoint CreateCheckpoint()
    {
        return new OutboxCheckpoint(
            this,
            _deferredMethods.CreateCheckpoint(),
            _outboxSchedulerContext?.CreateCheckpoint() ?? default);
    }

    /// <summary>Releases and executes all pending operations.</summary>
    /// <param name="concurrentMessageDelivery">Whether independent deferred sends may execute concurrently.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when all deferred operations and cancellations have finished.</returns>
    public virtual async Task ExecutePendingActionsAsync(bool concurrentMessageDelivery, CancellationToken cancellationToken = default)
    {
        _clearToSend.TrySetResult(this);

        await _deferredMethods.ExecuteAsync(concurrentMessageDelivery, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (_outboxSchedulerContext != null)
        {
            try
            {
                await _outboxSchedulerContext.ExecutePendingActionsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogContext.Warning?.Log(e, "One or more messages could not be unscheduled.", e);
            }
        }
    }

    /// <summary>Discards every pending operation and cancels every deferred scheduled message.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after pending operations are discarded and tracked schedules are canceled.</returns>
    public virtual async Task DiscardPendingActionsAsync(CancellationToken cancellationToken = default)
    {
        _deferredMethods.Discard(cancellationToken);

        if (_outboxSchedulerContext != null)
        {
            try
            {
                await _outboxSchedulerContext.CancelAllScheduledMessagesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                LogContext.Warning?.Log(e, "One or more messages could not be unscheduled.", e);
            }
        }
    }

    /// <summary>Discards operations and scheduled messages added after a checkpoint.</summary>
    /// <param name="checkpoint">The checkpoint that defines the state to retain.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when work added after the checkpoint has been discarded.</returns>
    public virtual async Task DiscardPendingActionsAsync(OutboxCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (!ReferenceEquals(checkpoint.Owner, this))
            throw new ArgumentException("The checkpoint belongs to a different outbox context.", nameof(checkpoint));

        cancellationToken.ThrowIfCancellationRequested();

        _deferredMethods.DiscardSince(checkpoint.DeferredMethodCount);

        if (_outboxSchedulerContext != null)
            await _outboxSchedulerContext.DiscardSinceAsync(checkpoint.SchedulerCheckpoint).ConfigureAwait(false);
    }
}


/// <summary>Exposes a typed consumed message while buffering its outgoing operations in memory.</summary>
/// <typeparam name="T">The consumed message contract.</typeparam>
public class InMemoryOutboxConsumeContext<T> :
    InMemoryOutboxConsumeContext,
    ConsumeContext<T>
    where T : class
{
    readonly ConsumeContext<T> _context;

    /// <summary>Initializes a typed in-memory outbox over an existing consume context.</summary>
    /// <param name="context">The typed consume context whose outgoing operations are deferred.</param>
    public InMemoryOutboxConsumeContext(ConsumeContext<T> context)
        : base((context ?? throw new ArgumentNullException(nameof(context))).Advanced())
    {
        _context = context;
    }

    /// <summary>Gets the consumed message.</summary>
    public T Message => _context.Message;

    /// <summary>Notifies observers that the message was consumed successfully.</summary>
    /// <param name="duration">The elapsed message-processing time.</param>
    /// <param name="consumerType">The display name of the consumer that processed the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The observer-notification task.</returns>
    public virtual Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Notifies observers that message consumption faulted.</summary>
    /// <param name="duration">The elapsed message-processing time.</param>
    /// <param name="consumerType">The display name of the consumer that attempted to process the message.</param>
    /// <param name="exception">The exception raised by the consumer.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The observer-notification task.</returns>
    public virtual Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }


    /// <summary>Adapts a consumed message batch to per-message in-memory outbox contexts.</summary>
    internal sealed class BatchContext :
        InMemoryOutboxConsumeContext,
        ConsumeContext<IMessageBatch<T>>
    {
        readonly MessageBatch<T> _batch;
        readonly List<InMemoryOutboxConsumeContext<T>> _messages;

        /// <summary>Initializes per-message outboxes for every message in a consumed batch.</summary>
        /// <param name="context">The batch consume context to decorate.</param>
        public BatchContext(ConsumeContext<IMessageBatch<T>> context)
            : base((context ?? throw new ArgumentNullException(nameof(context))).Advanced())
        {
            IMessageBatch<T> batch = context.Message;
            _messages = batch.Select(x => new InMemoryOutboxConsumeContext<T>(x)).ToList();
            _batch = new MessageBatch<T>(batch.FirstMessageReceived, batch.LastMessageReceived, batch.Mode, _messages);
        }

        /// <summary>Gets the batch whose message contexts are backed by child outboxes.</summary>
        public IMessageBatch<T> Message => _batch;

        /// <summary>Notifies observers that the batch was consumed successfully.</summary>
        /// <param name="duration">The elapsed batch-processing time.</param>
        /// <param name="consumerType">The display name of the consumer that processed the batch.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The observer-notification task.</returns>
        public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        {
            return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
        }

        /// <summary>Notifies observers that batch consumption faulted.</summary>
        /// <param name="duration">The elapsed batch-processing time.</param>
        /// <param name="consumerType">The display name of the consumer that attempted to process the batch.</param>
        /// <param name="exception">The exception raised by the consumer.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>The observer-notification task.</returns>
        public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        {
            return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
        }

        /// <summary>Executes pending operations for the batch and every message it contains.</summary>
        /// <param name="concurrentMessageDelivery">Whether independent deferred sends may execute concurrently.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that completes when the parent and child outboxes have finished delivery.</returns>
        public override async Task ExecutePendingActionsAsync(bool concurrentMessageDelivery, CancellationToken cancellationToken = default)
        {
            await base.ExecutePendingActionsAsync(concurrentMessageDelivery, cancellationToken: cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(_messages.Select(x => x.ExecutePendingActionsAsync(concurrentMessageDelivery, cancellationToken: cancellationToken))).ConfigureAwait(false);
        }

        /// <summary>Discards pending operations for the batch and every message it contains.</summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that completes after the parent and child outboxes have discarded pending work.</returns>
        public override async Task DiscardPendingActionsAsync(CancellationToken cancellationToken = default)
        {
            await base.DiscardPendingActionsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(_messages.Select(x => x.DiscardPendingActionsAsync(cancellationToken: cancellationToken))).ConfigureAwait(false);
        }

        /// <summary>Captures the current state of the batch outbox and every child outbox.</summary>
        /// <returns>A composite checkpoint that can be used to discard later additions.</returns>
        public override OutboxCheckpoint CreateCheckpoint()
        {
            OutboxCheckpoint parentCheckpoint = base.CreateCheckpoint();
            OutboxCheckpoint[] childCheckpoints = _messages.Select(message => message.CreateCheckpoint()).ToArray();

            return new OutboxCheckpoint(
                this,
                parentCheckpoint.DeferredMethodCount,
                parentCheckpoint.SchedulerCheckpoint,
                childCheckpoints);
        }

        /// <summary>Discards batch and child operations added after a composite checkpoint.</summary>
        /// <param name="checkpoint">The composite checkpoint that defines the state to retain.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that completes when work added after the parent and child checkpoints has been discarded.</returns>
        public override async Task DiscardPendingActionsAsync(OutboxCheckpoint checkpoint, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(checkpoint);
            if (!ReferenceEquals(checkpoint.Owner, this))
                throw new ArgumentException("The checkpoint belongs to a different outbox context.", nameof(checkpoint));
            if (checkpoint.ChildCheckpoints.Count != _messages.Count)
                throw new ArgumentException("The checkpoint does not describe this batch outbox.", nameof(checkpoint));

            await base.DiscardPendingActionsAsync(checkpoint, cancellationToken: cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(_messages.Select((message, index) =>
                message.DiscardPendingActionsAsync(checkpoint.ChildCheckpoints[index], cancellationToken: cancellationToken))).ConfigureAwait(false);
        }
    }
}
