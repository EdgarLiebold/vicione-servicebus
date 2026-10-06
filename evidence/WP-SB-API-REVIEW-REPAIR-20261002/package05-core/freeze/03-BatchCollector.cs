using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Batching.Runtime;

/// <summary>Owns the common admission, cancellation, probing, and shutdown lifecycle for batch collectors.</summary>
/// <typeparam name="TMessage">The message contract collected into each batch.</typeparam>
internal abstract class BatchCollectorBase<TMessage> :
    IBatchCollector<TMessage>
    where TMessage : class
{
    readonly IPipe<ConsumeContext<IMessageBatch<TMessage>>> _consumerPipe;
    readonly BatchCollectorLifetime _lifetime;
    readonly BatchRuntimeSettings _settings;

    /// <summary>Creates a collector whose completed batches are sent through the supplied pipe.</summary>
    /// <param name="options">The validated batch limits captured for this collector.</param>
    /// <param name="consumerPipe">The pipeline that receives each completed batch.</param>
    protected BatchCollectorBase(BatchOptions options, IPipe<ConsumeContext<IMessageBatch<TMessage>>> consumerPipe)
    {
        _settings = new BatchRuntimeSettings(options);
        _consumerPipe = consumerPipe ?? throw new ArgumentNullException(nameof(consumerPipe));
        _lifetime = new BatchCollectorLifetime(_settings.ConcurrencyLimit);
    }

    /// <summary>Stops admissions, delivers every partial batch, and drains all accepted operations.</summary>
    /// <returns>A value task that completes after collection and batch delivery have terminated.</returns>
    public ValueTask DisposeAsync() => _lifetime.DisposeAsync(FlushActiveBatchesAsync);

    /// <summary>Adds one message to the batch selected by the concrete collector.</summary>
    /// <param name="context">The message context to collect.</param>
    /// <param name="cancellationToken">Cancels admission to the serialized collector.</param>
    /// <returns>The batch consumer whose completion governs the message pipeline.</returns>
    public Task<BatchConsumer<TMessage>> CollectAsync(
        ConsumeContext<TMessage> context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<BatchConsumer<TMessage>>(cancellationToken);
        if (!_lifetime.TryBeginOperation())
            return Task.FromException<BatchConsumer<TMessage>>(new ObjectDisposedException(GetType().Name));

        return CollectCoreAsync(context, Activity.Current, cancellationToken);
    }

    /// <summary>Removes a completed batch from the active lookup when it still matches.</summary>
    /// <param name="consumer">The completed batch consumer.</param>
    /// <param name="cancellationToken">Cancels admission to the serialized collector.</param>
    /// <returns>A task that completes after the active-batch lookup has been updated.</returns>
    public Task CompleteAsync(
        BatchConsumer<TMessage> consumer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(consumer);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (!_lifetime.TryBeginOperation())
            return Task.CompletedTask;

        return CompleteCoreAsync(consumer, cancellationToken);
    }

    /// <summary>Adds the completed-batch pipeline to the probe graph.</summary>
    /// <param name="context">The probe graph to extend.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _consumerPipe.Probe(context.CreateScope("batchCollector"));
    }

    /// <summary>Adds a message to an existing batch or creates its successor.</summary>
    /// <param name="context">The message context to add.</param>
    /// <param name="currentBatch">The batch currently assigned to the message stream.</param>
    /// <param name="currentActivity">The trace activity associated with message admission.</param>
    /// <returns>The batch that now owns the message pipeline.</returns>
    protected async Task<BatchConsumer<TMessage>> CollectIntoBatchAsync(
        ConsumeContext<TMessage> context,
        BatchConsumer<TMessage>? currentBatch,
        Activity? currentActivity)
    {
        if (currentBatch != null && context.Advanced().GetRetryAttempt() > 0)
            await currentBatch.ForceCompleteAsync().ConfigureAwait(false);

        if (currentBatch == null || currentBatch.IsCompleted)
        {
            TimeProvider timeProvider = context.GetTimeProvider();
            if (ReferenceEquals(timeProvider, TimeProvider.System)
                && (long)_settings.TimeLimit.TotalMilliseconds > uint.MaxValue - 1L)
            {
                string message = $"Batch timing at endpoint '{context.Advanced().ReceiveContext.InputAddress}': "
                    + $"Batch.TimeLimit ({_settings.TimeLimit:c}) exceeds the System timer range of 4294967294 whole milliseconds. "
                    + "Set Batch.TimeLimit to a positive supported duration or attach a TimeProvider whose timer supports this interval.";
                var failure = global::ViciOne.ServiceBus.Configuration.ValidationResultExtensions.Failure(null, "Batch.TimeLimit", message);
                throw new ConfigurationException([failure], message);
            }

            currentBatch = new BatchConsumer<TMessage>(
                _settings,
                _lifetime.Collector,
                _lifetime.Dispatcher,
                _consumerPipe,
                timeProvider);
        }

        await currentBatch.AddAsync(context, currentActivity).ConfigureAwait(false);
        return currentBatch;
    }

    /// <summary>Adds a message to the concrete collector's active-batch lookup.</summary>
    protected abstract Task<BatchConsumer<TMessage>> AddAsync(ConsumeContext<TMessage> context, Activity? currentActivity);

    /// <summary>Removes a completed batch from the concrete collector's active-batch lookup.</summary>
    protected abstract Task RemoveAsync(BatchConsumer<TMessage> consumer);

    /// <summary>Closes and detaches every batch still owned by the concrete collector.</summary>
    protected abstract Task FlushActiveBatchesAsync();

    async Task<BatchConsumer<TMessage>> CollectCoreAsync(
        ConsumeContext<TMessage> context,
        Activity? currentActivity,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource? linkedCancellation = CreateOperationCancellation(
            context.CancellationToken,
            cancellationToken,
            out CancellationToken operationToken);
        try
        {
            return await _lifetime.Collector.ExecuteAsync(
                    () => AddAsync(context, currentActivity),
                    operationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(exception.Message, exception, cancellationToken);
        }
        catch (OperationCanceledException exception) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(exception.Message, exception, context.CancellationToken);
        }
        finally
        {
            _lifetime.CompleteOperation();
        }
    }

    async Task CompleteCoreAsync(
        BatchConsumer<TMessage> consumer,
        CancellationToken cancellationToken)
    {
        try
        {
            await _lifetime.Collector.ExecuteAsync(
                    () => RemoveAsync(consumer),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _lifetime.CompleteOperation();
        }
    }

    static CancellationTokenSource? CreateOperationCancellation(
        CancellationToken contextCancellationToken,
        CancellationToken cancellationToken,
        out CancellationToken operationToken)
    {
        if (!contextCancellationToken.CanBeCanceled || contextCancellationToken == cancellationToken)
        {
            operationToken = cancellationToken;
            return null;
        }

        if (!cancellationToken.CanBeCanceled)
        {
            operationToken = contextCancellationToken;
            return null;
        }

        CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(contextCancellationToken, cancellationToken);
        operationToken = linkedCancellation.Token;
        return linkedCancellation;
    }
}

/// <summary>Serializes messages into consecutive batches without a grouping key.</summary>
/// <typeparam name="TMessage">The message contract collected into each batch.</typeparam>
internal sealed class BatchCollector<TMessage> :
    BatchCollectorBase<TMessage>
    where TMessage : class
{
    BatchConsumer<TMessage>? _currentBatch;

    /// <summary>Creates a collector whose completed batches are sent through the supplied pipe.</summary>
    /// <param name="options">The validated batch limits captured for this collector.</param>
    /// <param name="consumerPipe">The pipeline that receives each completed batch.</param>
    public BatchCollector(BatchOptions options, IPipe<ConsumeContext<IMessageBatch<TMessage>>> consumerPipe)
        : base(options, consumerPipe)
    {
    }

    protected override async Task<BatchConsumer<TMessage>> AddAsync(
        ConsumeContext<TMessage> context,
        Activity? currentActivity)
    {
        _currentBatch = await CollectIntoBatchAsync(context, _currentBatch, currentActivity).ConfigureAwait(false);
        return _currentBatch;
    }

    protected override Task RemoveAsync(BatchConsumer<TMessage> consumer)
    {
        if (_currentBatch == consumer)
            _currentBatch = null;

        return Task.CompletedTask;
    }

    protected override Task FlushActiveBatchesAsync()
    {
        BatchConsumer<TMessage>? batch = _currentBatch;
        _currentBatch = null;
        return batch?.ForceCompleteAsync() ?? Task.CompletedTask;
    }
}

/// <summary>Serializes messages into independent batches selected by a grouping key.</summary>
/// <typeparam name="TMessage">The message contract collected into each batch.</typeparam>
/// <typeparam name="TKey">The non-null grouping-key type.</typeparam>
internal sealed class BatchCollector<TMessage, TKey> :
    BatchCollectorBase<TMessage>
    where TMessage : class
    where TKey : notnull
{
    readonly Dictionary<TKey, BatchConsumer<TMessage>> _batchesByKey = [];
    readonly IGroupKeyProvider<TMessage, TKey> _keyProvider;
    BatchConsumer<TMessage>? _ungroupedBatch;

    /// <summary>Creates a collector whose completed groups are sent through the supplied pipe.</summary>
    /// <param name="options">The validated batch limits captured for this collector.</param>
    /// <param name="consumerPipe">The pipeline that receives each completed batch.</param>
    /// <param name="keyProvider">The selector that assigns messages to groups.</param>
    public BatchCollector(
        BatchOptions options,
        IPipe<ConsumeContext<IMessageBatch<TMessage>>> consumerPipe,
        IGroupKeyProvider<TMessage, TKey> keyProvider)
        : base(RequireKeyProvider(options, keyProvider), consumerPipe)
    {
        _keyProvider = keyProvider;
    }

    protected override async Task<BatchConsumer<TMessage>> AddAsync(
        ConsumeContext<TMessage> context,
        Activity? currentActivity)
    {
        if (_keyProvider.TryGetKey(context, out TKey key))
        {
            _batchesByKey.TryGetValue(key, out BatchConsumer<TMessage>? batch);
            batch = await CollectIntoBatchAsync(context, batch, currentActivity).ConfigureAwait(false);
            _batchesByKey[key] = batch;
            return batch;
        }

        _ungroupedBatch = await CollectIntoBatchAsync(context, _ungroupedBatch, currentActivity).ConfigureAwait(false);
        return _ungroupedBatch;
    }

    protected override Task RemoveAsync(BatchConsumer<TMessage> consumer)
    {
        if (_ungroupedBatch == consumer)
        {
            _ungroupedBatch = null;
            return Task.CompletedTask;
        }

        TKey? matchingKey = default;
        var found = false;
        foreach (KeyValuePair<TKey, BatchConsumer<TMessage>> entry in _batchesByKey)
        {
            if (entry.Value != consumer)
                continue;

            matchingKey = entry.Key;
            found = true;
            break;
        }

        if (found)
            _batchesByKey.Remove(matchingKey!);

        return Task.CompletedTask;
    }

    protected override Task FlushActiveBatchesAsync()
    {
        BatchConsumer<TMessage>[] batches = _batchesByKey.Values
            .Append(_ungroupedBatch)
            .OfType<BatchConsumer<TMessage>>()
            .Distinct()
            .ToArray();
        _batchesByKey.Clear();
        _ungroupedBatch = null;

        return Task.WhenAll(batches.Select(static batch => batch.ForceCompleteAsync()));
    }

    static BatchOptions RequireKeyProvider(BatchOptions options, IGroupKeyProvider<TMessage, TKey> keyProvider)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);
        return options;
    }
}
