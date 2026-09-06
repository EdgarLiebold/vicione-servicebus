using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Consumes batch messages.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class BatchConsumer<TMessage> :
    IConsumer<TMessage>
    where TMessage : class
{
    readonly TaskCompletionSource<DateTime> _completed;
    readonly IPipe<ConsumeContext<Batch<TMessage>>> _consumerPipe;
    readonly TaskExecutor _dispatcher;
    readonly TaskExecutor _executor;
    readonly DateTime _firstMessage;
    readonly Dictionary<Guid, BatchEntry> _messages;
    readonly BatchOptions _options;
    readonly ITimer _timer;
    readonly TimeProvider _timeProvider;
    Activity _currentActivity = null!;
    DateTime _lastMessage;
    ILogContext? _logContext = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="executor">The executor.</param>
    /// <param name="dispatcher">The dispatcher.</param>
    /// <param name="consumerPipe">The consumer pipe.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public BatchConsumer(BatchOptions options, TaskExecutor executor, TaskExecutor dispatcher, IPipe<ConsumeContext<Batch<TMessage>>> consumerPipe,
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _executor = executor;
        _consumerPipe = consumerPipe;
        _dispatcher = dispatcher;
        _messages = new Dictionary<Guid, BatchEntry>();
        _completed = TaskCompletionSources.Create<DateTime>();
        _firstMessage = _timeProvider.GetUtcNow().UtcDateTime;
        _options = options;

        _timer = _timeProvider.CreateTimer(TimeLimitExpired, null, _options.TimeLimit, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Gets or sets a value indicating whether completed.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<TMessage> context)
    {
        try
        {
            await _completed.Task.ConfigureAwait(false);
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
        _executor.EnqueueBlocking(() =>
        {
            if (IsCompleted)
                return Task.CompletedTask;

            IsCompleted = true;

            if (_messages.Count <= 0)
                return Task.CompletedTask;

            List<ConsumeContext<TMessage>> messages = GetMessageBatchInOrder();

            return _dispatcher.EnqueueAsync(() => DeliverAsync(messages[messages.Count - 1].Advanced(), messages, BatchCompletionMode.Time));
        });
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="currentActivity">The current activity.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddAsync(ConsumeContext<TMessage> context, Activity? currentActivity, CancellationToken cancellationToken = default)
    {
        _logContext ??= LogContext.Current;
        if (currentActivity != null)
            _currentActivity = currentActivity;

        var messageId = context.MessageId ?? NewId.NextGuid();

        ulong? sequenceNumber = context.Advanced().ReceiveContext.TryGetPayload<ITransportSequenceNumber>(out var payload)
            ? payload.SequenceNumber
            : null;
        ulong sentTimeAsSequenceFallback() => (ulong)(context.SentTime ?? context.Advanced().ReceiveContext.GetSentTime()
            ?? _timeProvider.GetUtcNow().UtcDateTime).Ticks;

        var batchEntry = new BatchEntry(
            context,
            sequenceNumber ?? sentTimeAsSequenceFallback(),
            () => RemoveCanceledMessage(messageId));

        if (!_messages.ContainsKey(messageId))
            _messages.Add(messageId, batchEntry);
        else
            batchEntry.Unregister();

        if (_options.TimeLimitStart == BatchTimeLimitStart.FromLast)
            _timer.Change(_options.TimeLimit, TimeSpan.FromMilliseconds(-1));

        _lastMessage = _timeProvider.GetUtcNow().UtcDateTime;

        if (IsReadyToDeliver(context.Advanced()))
        {
            IsCompleted = true;

            List<ConsumeContext<TMessage>> messageList = GetMessageBatchInOrder();

            return messageList.Count == 0
                ? Task.CompletedTask
                : _dispatcher.EnqueueAsync(() => DeliverAsync(context.Advanced(), messageList, BatchCompletionMode.Size), cancellationToken: cancellationToken);
        }

        return Task.CompletedTask;
    }

    void RemoveCanceledMessage(Guid messageId)
    {
        _executor.EnqueueBlocking(() =>
        {
            if (IsCompleted)
                return Task.CompletedTask;

            if (_messages.TryGetValue(messageId, out var batchEntry))
            {
                batchEntry.Unregister();

                _messages.Remove(messageId);

                if (_messages.Count == 0)
                {
                    IsCompleted = true;

                    _completed.TrySetCanceled();
                }
            }

            return Task.CompletedTask;
        });
    }

    bool IsReadyToDeliver(ConsumeContext context)
    {
        if (context.GetRetryAttempt() > 0)
            return true;

        return _messages.Count == _options.MessageLimit;
    }

    /// <summary>Forces complete.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ForceCompleteAsync(CancellationToken cancellationToken = default)
    {
        IsCompleted = true;

        List<ConsumeContext<TMessage>> consumeContexts = GetMessageBatchInOrder();
        return consumeContexts.Count == 0
            ? Task.CompletedTask
            : _dispatcher.EnqueueAsync(() => DeliverAsync(consumeContexts[consumeContexts.Count - 1].Advanced(), consumeContexts,
                BatchCompletionMode.Forced), cancellationToken: cancellationToken);
    }

    async Task DeliverAsync(ConsumeContext context, IReadOnlyList<ConsumeContext<TMessage>> messages, BatchCompletionMode batchCompletionMode)
    {
        _timer.Dispose();

        foreach (var batchEntry in _messages.Values)
            batchEntry.Unregister();

        LogContext.SetCurrentIfNull(_logContext);

        Activity.Current = _currentActivity;

        Batch<TMessage> batch = new MessageBatch<TMessage>(_firstMessage, _lastMessage, batchCompletionMode, messages);

        ConsumeContext<Batch<TMessage>> batchConsumeContext = new BatchConsumeContext<TMessage>(context, batch);

        try
        {
            await _consumerPipe.SendAsync(batchConsumeContext).ConfigureAwait(false);

            _completed.TrySetResult(_timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (OperationCanceledException exception) when (exception.CancellationToken == context.CancellationToken)
        {
            _completed.TrySetCanceled();
        }
        catch (Exception exception)
        {
            if (batchConsumeContext.TryGetPayload(out RetryContext<ConsumeContext<Batch<TMessage>>>? retryContext))
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
