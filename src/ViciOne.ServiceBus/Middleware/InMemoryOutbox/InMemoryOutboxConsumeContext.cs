using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Batching;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Middleware.InMemoryOutbox;

/// <summary>Carries state for in memory outbox consume operations.</summary>
public class InMemoryOutboxConsumeContext :
    ConsumeContextProxy,
    OutboxContext
{
    readonly TaskCompletionSource<InMemoryOutboxConsumeContext> _clearToSend;
    readonly InMemoryOutboxDeferredMethodCollection _deferredMethods;
    readonly InMemoryOutboxMessageSchedulerContext _outboxSchedulerContext = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected InMemoryOutboxConsumeContext(ConsumeContext context)
        : base(context)
    {
        CapturedContext = context;

        var outboxReceiveContext = new InMemoryOutboxReceiveContext(this, context.ReceiveContext);

        ReceiveContext = outboxReceiveContext;
        PublishEndpointProvider = outboxReceiveContext.PublishEndpointProvider;

        _clearToSend = TaskCompletionSources.Create<InMemoryOutboxConsumeContext>();

        _deferredMethods = new InMemoryOutboxDeferredMethodCollection(_clearToSend.Task);

        if (context.TryGetPayload(out MessageSchedulerContext? schedulerContext))
        {
            _outboxSchedulerContext = (InMemoryOutboxMessageSchedulerContext)context.AddOrUpdatePayload<MessageSchedulerContext>(
                () => new InMemoryOutboxMessageSchedulerContext(context, schedulerContext.SchedulerFactory, _clearToSend.Task),
                existing => new InMemoryOutboxMessageSchedulerContext(context, existing.SchedulerFactory, _clearToSend.Task));
        }
    }

    /// <summary>Gets the captured context.</summary>
    public ConsumeContext CapturedContext { get; }

    /// <summary>Gets the clear to send.</summary>
    public Task ClearToSend => _clearToSend.Task;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="method">The method.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddAsync(Func<Task> method, CancellationToken cancellationToken = default)
    {
        return _deferredMethods.AddAsync(method, cancellationToken: cancellationToken);
    }

    /// <summary>Creates checkpoint.</summary>
    /// <returns>The created checkpoint.</returns>
    public virtual OutboxCheckpoint CreateCheckpoint()
    {
        return new OutboxCheckpoint(
            this,
            _deferredMethods.CreateCheckpoint(),
            _outboxSchedulerContext?.CreateCheckpoint() ?? default);
    }

    /// <summary>Executes pending actions.</summary>
    /// <param name="concurrentMessageDelivery">The concurrent message delivery.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Discards pending actions.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Discards pending actions.</summary>
    /// <param name="checkpoint">The checkpoint.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual async Task DiscardPendingActionsAsync(OutboxCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(checkpoint);
        if (!ReferenceEquals(checkpoint.Owner, this))
            throw new ArgumentException("The checkpoint belongs to a different outbox context.", nameof(checkpoint));

        _deferredMethods.DiscardSince(checkpoint.DeferredMethodCount);

        if (_outboxSchedulerContext != null)
            await _outboxSchedulerContext.DiscardSinceAsync(checkpoint.SchedulerCheckpoint).ConfigureAwait(false);
    }
}


/// <summary>Carries state for in memory outbox consume operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class InMemoryOutboxConsumeContext<T> :
    InMemoryOutboxConsumeContext,
    ConsumeContext<T>
    where T : class
{
    readonly ConsumeContext<T> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public InMemoryOutboxConsumeContext(ConsumeContext<T> context)
        : base(context.Advanced())
    {
        _context = context;
    }

    /// <summary>Gets the message.</summary>
    public T Message => _context.Message;

    /// <summary>Reports that notify has been consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }


    /// <summary>Adapts a consumed message batch to per-message in-memory outbox contexts.</summary>
    public class Batch :
        InMemoryOutboxConsumeContext,
        ConsumeContext<Batch<T>>
    {
        readonly MessageBatch<T> _batch;
        readonly List<InMemoryOutboxConsumeContext<T>> _messages;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="context">The context associated with the operation.</param>
        public Batch(ConsumeContext<Batch<T>> context)
            : base(context.Advanced())
        {
            Batch<T> batch = context.Message;
            _messages = batch.Select(x => new InMemoryOutboxConsumeContext<T>(x)).ToList();
            _batch = new MessageBatch<T>(batch.FirstMessageReceived, batch.LastMessageReceived, batch.Mode, _messages);
        }

        /// <summary>Gets the message.</summary>
        public Batch<T> Message => _batch;

        /// <summary>Reports that notify has been consumed.</summary>
        /// <param name="duration">The duration.</param>
        /// <param name="consumerType">The runtime consumer type used by the operation.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        {
            return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
        }

        /// <summary>Reports that notify has faulted.</summary>
        /// <param name="duration">The duration.</param>
        /// <param name="consumerType">The runtime consumer type used by the operation.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        {
            return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
        }

        /// <summary>Executes pending actions.</summary>
        /// <param name="concurrentMessageDelivery">The concurrent message delivery.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task ExecutePendingActionsAsync(bool concurrentMessageDelivery, CancellationToken cancellationToken = default)
        {
            await base.ExecutePendingActionsAsync(concurrentMessageDelivery, cancellationToken: cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(_messages.Select(x => x.ExecutePendingActionsAsync(concurrentMessageDelivery, cancellationToken: cancellationToken))).ConfigureAwait(false);
        }

        /// <summary>Discards pending actions.</summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public override async Task DiscardPendingActionsAsync(CancellationToken cancellationToken = default)
        {
            await base.DiscardPendingActionsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(_messages.Select(x => x.DiscardPendingActionsAsync(cancellationToken: cancellationToken))).ConfigureAwait(false);
        }

        /// <summary>Creates checkpoint.</summary>
        /// <returns>The created checkpoint.</returns>
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

        /// <summary>Discards pending actions.</summary>
        /// <param name="checkpoint">The checkpoint.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
