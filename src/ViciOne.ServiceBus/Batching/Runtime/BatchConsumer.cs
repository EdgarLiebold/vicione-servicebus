using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Batching.Contexts;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Batching.Runtime;

/// <summary>Tracks one batch until size, time, cancellation, or terminal flushing selects its outcome.</summary>
/// <typeparam name="TMessage">The message contract collected into the batch.</typeparam>
internal sealed class BatchConsumer<TMessage> :
    IConsumer<TMessage>
    where TMessage : class
{
    readonly TaskCompletionSource<DateTime> _completed;
    readonly IPipe<ConsumeContext<Batch<TMessage>>> _consumerPipe;
    readonly TaskExecutor _dispatcher;
    readonly TaskExecutor _executor;
    readonly DateTime _firstMessage;
    readonly Dictionary<Guid, BatchEntry> _messages;
    readonly BatchRuntimeSettings _settings;
    readonly ITimer _timer;
    readonly TimeProvider _timeProvider;
    int _completionState;
    Activity? _currentActivity;
    DateTime _lastMessage;
    ILogContext? _logContext;

    /// <summary>Creates an empty batch and starts its configured completion timer.</summary>
    /// <param name="settings">The immutable batch size and timing limits.</param>
    /// <param name="executor">The executor that serializes changes to this batch.</param>
    /// <param name="dispatcher">The executor that bounds completed-batch delivery.</param>
    /// <param name="consumerPipe">The pipeline that receives this batch after completion.</param>
    /// <param name="timeProvider">The clock and timer source for batch metadata and expiration.</param>
    public BatchConsumer(BatchRuntimeSettings settings, TaskExecutor executor, TaskExecutor dispatcher, IPipe<ConsumeContext<Batch<TMessage>>> consumerPipe,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(consumerPipe);

        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _executor = executor;
        _consumerPipe = consumerPipe;
        _dispatcher = dispatcher;
        _messages = new Dictionary<Guid, BatchEntry>();
        _completed = TaskCompletionSources.Create<DateTime>();
        _firstMessage = _timeProvider.GetUtcNow().UtcDateTime;
        _settings = settings;

        _timer = _timeProvider.CreateTimer(TimeLimitExpired, null, _settings.TimeLimit, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Gets whether this collector has stopped accepting messages and chosen its terminal outcome.</summary>
    public bool IsCompleted => Volatile.Read(ref _completionState) != 0;

    /// <summary>Holds one message pipeline open until this batch reaches its terminal outcome.</summary>
    /// <param name="context">The message context waiting on the batch.</param>
    /// <returns>A task that completes with the batch delivery outcome.</returns>
    public async Task ConsumeAsync(ConsumeContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _completed.Task.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(context.CancellationToken);
        }
        catch
        {
            // Successfully delivered messages are excluded from the batch fault set.
            if (context.Advanced().ReceiveContext.IsDelivered)
                return;

            throw;
        }
    }

    void TimeLimitExpired(object? state)
    {
        if (IsCompleted)
            return;

        try
        {
            _executor.EnqueueBlocking(() => CompleteBatchAsync(BatchCompletionMode.Time));
        }
        catch (ObjectDisposedException) when (IsCompleted)
        {
            // Terminal cleanup already owns the batch and has stopped the collector executor.
        }
    }

    /// <summary>Adds a message and closes the batch immediately when its size limit is reached.</summary>
    /// <param name="context">The message context to add.</param>
    /// <param name="currentActivity">The trace activity associated with the admission.</param>
    /// <param name="cancellationToken">Cancels admission of a size-completed batch to the delivery queue.</param>
    /// <returns>A task that completes after any required delivery-queue admission.</returns>
    public Task AddAsync(ConsumeContext<TMessage> context, Activity? currentActivity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logContext ??= LogContext.Current;
        if (currentActivity != null)
            _currentActivity = currentActivity;

        var messageId = context.MessageId ?? NewId.NextGuid();

        ulong? sequenceNumber = context.Advanced().ReceiveContext.TryGetPayload<ITransportSequenceNumber>(out var payload)
            ? payload.SequenceNumber
            : null;
        ulong GetSentTimeAsSequenceFallback() => (ulong)(context.SentTime ?? context.Advanced().ReceiveContext.GetSentTime()
            ?? _timeProvider.GetUtcNow().UtcDateTime).Ticks;

        var batchEntry = new BatchEntry(
            context,
            sequenceNumber ?? GetSentTimeAsSequenceFallback(),
            () => RemoveCanceledMessage(messageId));

        if (!_messages.ContainsKey(messageId))
            _messages.Add(messageId, batchEntry);
        else
            batchEntry.Unregister();

        if (_settings.TimeLimitStart == BatchTimeLimitStart.FromLast)
            _timer.Change(_settings.TimeLimit, Timeout.InfiniteTimeSpan);

        _lastMessage = _timeProvider.GetUtcNow().UtcDateTime;

        if (IsReadyToDeliver(context.Advanced()))
            return CompleteBatchAsync(BatchCompletionMode.Size, context.Advanced(), cancellationToken);

        return Task.CompletedTask;
    }

    void RemoveCanceledMessage(Guid messageId)
    {
        if (IsCompleted)
            return;

        try
        {
            _executor.EnqueueBlocking(() =>
            {
                if (IsCompleted)
                    return Task.CompletedTask;

                if (_messages.Remove(messageId, out BatchEntry batchEntry))
                {
                    batchEntry.Unregister();

                    if (_messages.Count == 0 && Interlocked.CompareExchange(ref _completionState, 1, 0) == 0)
                    {
                        Exception? cleanupFailure = StopTimerAndRegistrations();
                        if (cleanupFailure == null)
                            _completed.TrySetCanceled(batchEntry.Context.CancellationToken);
                        else
                        {
                            _completed.TrySetException(cleanupFailure);
                            return Task.FromException(cleanupFailure);
                        }
                    }
                }

                return Task.CompletedTask;
            });
        }
        catch (ObjectDisposedException) when (IsCompleted)
        {
            // Terminal cleanup already owns the batch and has stopped the collector executor.
        }
    }

    bool IsReadyToDeliver(ConsumeContext context)
    {
        if (context.GetRetryAttempt() > 0)
            return true;

        return _messages.Count >= _settings.MessageLimit;
    }

    /// <summary>Closes the current partial batch and schedules it for immediate delivery.</summary>
    /// <param name="cancellationToken">Cancels only admission to the batch-dispatch queue.</param>
    /// <returns>A task that completes after the forced batch is admitted to the dispatch queue.</returns>
    public Task ForceCompleteAsync(CancellationToken cancellationToken = default)
    {
        return CompleteBatchAsync(BatchCompletionMode.Forced, cancellationToken: cancellationToken);
    }

    Task CompleteBatchAsync(
        BatchCompletionMode completionMode,
        ConsumeContext? completionContext = null,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _completionState, 1, 0) != 0)
            return Task.CompletedTask;

        List<ConsumeContext<TMessage>> messages = GetMessageBatchInOrder();
        Exception? cleanupFailure = StopTimerAndRegistrations();
        _messages.Clear();
        if (cleanupFailure != null)
        {
            _completed.TrySetException(cleanupFailure);
            return Task.FromException(cleanupFailure);
        }

        if (messages.Count == 0)
        {
            _completed.TrySetResult(_timeProvider.GetUtcNow().UtcDateTime);
            return Task.CompletedTask;
        }

        ConsumeContext context = completionContext ?? messages[messages.Count - 1].Advanced();
        return EnqueueDeliveryAsync(context, messages, completionMode, cancellationToken);
    }

    async Task EnqueueDeliveryAsync(
        ConsumeContext context,
        IReadOnlyList<ConsumeContext<TMessage>> messages,
        BatchCompletionMode completionMode,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dispatcher.EnqueueAsync(
                    () => DeliverAsync(context, messages, completionMode),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            _completed.TrySetCanceled(exception.CancellationToken);
            throw;
        }
        catch (Exception exception)
        {
            _completed.TrySetException(exception);
            throw;
        }
    }

    Exception? StopTimerAndRegistrations()
    {
        List<Exception>? failures = null;
        try
        {
            _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        try
        {
            _timer.Dispose();
        }
        catch (Exception exception)
        {
            (failures ??= []).Add(exception);
        }

        foreach (BatchEntry batchEntry in _messages.Values)
        {
            try
            {
                batchEntry.Unregister();
            }
            catch (Exception exception)
            {
                (failures ??= []).Add(exception);
            }
        }

        return failures switch
        {
            null => null,
            { Count: 1 } => failures[0],
            _ => new AggregateException("Batch cleanup encountered multiple failures.", failures),
        };
    }

    async Task DeliverAsync(ConsumeContext context, IReadOnlyList<ConsumeContext<TMessage>> messages, BatchCompletionMode batchCompletionMode)
    {
        ConsumeContext<Batch<TMessage>>? batchConsumeContext = null;

        try
        {
            LogContext.SetCurrentIfNull(_logContext);
            Activity.Current = _currentActivity;

            Batch<TMessage> batch = new MessageBatch<TMessage>(_firstMessage, _lastMessage, batchCompletionMode, messages);
            batchConsumeContext = new BatchConsumeContext<TMessage>(context, batch);

            await _consumerPipe.SendAsync(batchConsumeContext).ConfigureAwait(false);

            _completed.TrySetResult(_timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == context.CancellationToken)
        {
            _completed.TrySetCanceled(context.CancellationToken);
        }
        catch (Exception exception)
        {
            if (batchConsumeContext != null
                && batchConsumeContext.TryGetPayload(out RetryContext<ConsumeContext<Batch<TMessage>>>? retryContext))
            {
                for (var i = 0; i < messages.Count; i++)
                    messages[i].GetOrAddPayload(() => retryContext);
            }

            _completed.TrySetException(exception);
        }
    }

    List<ConsumeContext<TMessage>> GetMessageBatchInOrder()
    {
        return _messages.Values.OrderBy(x => x.Index).Select(x => x.Context).ToList();
    }


    readonly struct BatchEntry
    {
        public readonly ConsumeContext<TMessage> Context;
        public readonly ulong Index;
        readonly CancellationTokenRegistration _registration;

        public BatchEntry(ConsumeContext<TMessage> context, ulong index, Action canceled)
        {
            Context = context;
            Index = index;

            if (context.CancellationToken.CanBeCanceled)
                _registration = context.CancellationToken.Register(() => canceled());
        }

        public void Unregister()
        {
            _registration.Dispose();
        }
    }
}
