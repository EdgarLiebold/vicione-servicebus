using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Batching;

/// <summary>Serializes messages into consecutive batches without a grouping key.</summary>
/// <typeparam name="TMessage">The message contract collected into each batch.</typeparam>
internal sealed class BatchCollector<TMessage> :
    IBatchCollector<TMessage>
    where TMessage : class
{
    readonly IPipe<ConsumeContext<Batch<TMessage>>> _consumerPipe;
    readonly BatchCollectorLifetime _lifetime;
    readonly BatchOptions _options;
    BatchConsumer<TMessage>? _currentConsumer;

    /// <summary>Creates a collector whose completed batches are sent through the supplied pipe.</summary>
    /// <param name="options">The batch size, timing, and delivery-concurrency limits.</param>
    /// <param name="consumerPipe">The pipeline that receives each completed batch.</param>
    public BatchCollector(BatchOptions options, IPipe<ConsumeContext<Batch<TMessage>>> consumerPipe)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _consumerPipe = consumerPipe ?? throw new ArgumentNullException(nameof(consumerPipe));

        _lifetime = new BatchCollectorLifetime(options.ConcurrencyLimit);
    }

    /// <summary>Stops admissions, delivers any partial batch, and drains all accepted operations.</summary>
    /// <returns>A value task that completes after collection and batch delivery have terminated.</returns>
    public ValueTask DisposeAsync() => _lifetime.DisposeAsync(FlushActiveBatchesAsync);

    /// <summary>Adds one message to the current batch.</summary>
    /// <param name="context">The message context to collect.</param>
    /// <param name="cancellationToken">Cancels admission to the serialized collector.</param>
    /// <returns>The batch consumer whose completion governs the message pipeline.</returns>
    public Task<BatchConsumer<TMessage>> CollectAsync(ConsumeContext<TMessage> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<BatchConsumer<TMessage>>(cancellationToken);
        if (!_lifetime.TryBeginOperation())
            return Task.FromException<BatchConsumer<TMessage>>(new ObjectDisposedException(GetType().Name));

        return CollectCoreAsync(context, Activity.Current, cancellationToken);
    }

    /// <summary>Removes a completed consumer when it still represents the current batch.</summary>
    /// <param name="context">The message context associated with the completed consumer.</param>
    /// <param name="consumer">The completed batch consumer.</param>
    /// <param name="cancellationToken">Cancels admission to the serialized collector.</param>
    /// <returns>A task that completes after the current-batch reference has been updated.</returns>
    public Task CompleteAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
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

        var scope = context.CreateScope("batchCollector");

        _consumerPipe.Probe(scope);
    }

    Task RemoveAsync(BatchConsumer<TMessage> consumer)
    {
        if (_currentConsumer == consumer)
            _currentConsumer = null;

        return Task.CompletedTask;
    }

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

    async Task CompleteCoreAsync(BatchConsumer<TMessage> consumer, CancellationToken cancellationToken)
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

    Task FlushActiveBatchesAsync()
    {
        BatchConsumer<TMessage>? consumer = _currentConsumer;
        _currentConsumer = null;
        return consumer?.ForceCompleteAsync() ?? Task.CompletedTask;
    }

    async Task<BatchConsumer<TMessage>> AddAsync(ConsumeContext<TMessage> context, Activity? currentActivity)
    {
        if (_currentConsumer != null)
        {
            if (context.Advanced().GetRetryAttempt() > 0)
                await _currentConsumer.ForceCompleteAsync().ConfigureAwait(false);
        }

        if (_currentConsumer == null || _currentConsumer.IsCompleted)
        {
            _currentConsumer = new BatchConsumer<TMessage>(
                _options,
                _lifetime.Collector,
                _lifetime.Dispatcher,
                _consumerPipe,
                context.GetTimeProvider());
        }

        await _currentConsumer.AddAsync(context, currentActivity).ConfigureAwait(false);

        return _currentConsumer;
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


/// <summary>Serializes messages into independent batches selected by a grouping key.</summary>
/// <typeparam name="TMessage">The message contract collected into each batch.</typeparam>
/// <typeparam name="TKey">The non-null grouping-key type.</typeparam>
internal sealed class BatchCollector<TMessage, TKey> :
    IBatchCollector<TMessage>
    where TMessage : class
    where TKey : notnull
{
    readonly IDictionary<TKey, BatchConsumer<TMessage>> _collectors;
    readonly IPipe<ConsumeContext<Batch<TMessage>>> _consumerPipe;
    readonly IGroupKeyProvider<TMessage, TKey> _keyProvider;
    readonly BatchCollectorLifetime _lifetime;
    readonly BatchOptions _options;
    BatchConsumer<TMessage>? _currentConsumer;

    /// <summary>Creates a collector whose completed groups are sent through the supplied pipe.</summary>
    /// <param name="options">The batch size, timing, and delivery-concurrency limits.</param>
    /// <param name="consumerPipe">The pipeline that receives each completed batch.</param>
    /// <param name="keyProvider">The selector that assigns messages to groups.</param>
    public BatchCollector(BatchOptions options, IPipe<ConsumeContext<Batch<TMessage>>> consumerPipe, IGroupKeyProvider<TMessage, TKey> keyProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _consumerPipe = consumerPipe ?? throw new ArgumentNullException(nameof(consumerPipe));
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));

        _lifetime = new BatchCollectorLifetime(options.ConcurrencyLimit);
        _collectors = new Dictionary<TKey, BatchConsumer<TMessage>>();
    }

    /// <summary>Stops admissions, delivers every partial group, and drains all accepted operations.</summary>
    /// <returns>A value task that completes after collection and batch delivery have terminated.</returns>
    public ValueTask DisposeAsync() => _lifetime.DisposeAsync(FlushActiveBatchesAsync);

    /// <summary>Adds one message to the batch selected by its grouping key.</summary>
    /// <param name="context">The message context to collect.</param>
    /// <param name="cancellationToken">Cancels admission to the serialized collector.</param>
    /// <returns>The batch consumer whose completion governs the message pipeline.</returns>
    public Task<BatchConsumer<TMessage>> CollectAsync(ConsumeContext<TMessage> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<BatchConsumer<TMessage>>(cancellationToken);
        if (!_lifetime.TryBeginOperation())
            return Task.FromException<BatchConsumer<TMessage>>(new ObjectDisposedException(GetType().Name));

        return CollectCoreAsync(context, Activity.Current, cancellationToken);
    }

    /// <summary>Removes a completed consumer when it still represents its group.</summary>
    /// <param name="context">The message context used to resolve the group.</param>
    /// <param name="consumer">The completed batch consumer.</param>
    /// <param name="cancellationToken">Cancels admission to the serialized collector.</param>
    /// <returns>A task that completes after the group reference has been updated.</returns>
    public Task CompleteAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(consumer);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (!_lifetime.TryBeginOperation())
            return Task.CompletedTask;

        return CompleteCoreAsync(context, consumer, cancellationToken);
    }

    /// <summary>Adds the completed-batch pipeline to the probe graph.</summary>
    /// <param name="context">The probe graph to extend.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("batchCollector");

        _consumerPipe.Probe(scope);
    }

    Task RemoveAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer)
    {
        if (_currentConsumer == consumer)
            _currentConsumer = null;
        else if (_keyProvider.TryGetKey(context, out var key) && _collectors.TryGetValue(key, out BatchConsumer<TMessage>? existingConsumer))
        {
            if (existingConsumer == consumer)
                _collectors.Remove(key);
        }

        return Task.CompletedTask;
    }

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
        ConsumeContext<TMessage> context,
        BatchConsumer<TMessage> consumer,
        CancellationToken cancellationToken)
    {
        try
        {
            await _lifetime.Collector.ExecuteAsync(
                    () => RemoveAsync(context, consumer),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _lifetime.CompleteOperation();
        }
    }

    Task FlushActiveBatchesAsync()
    {
        BatchConsumer<TMessage>[] consumers = _collectors.Values
            .Append(_currentConsumer)
            .OfType<BatchConsumer<TMessage>>()
            .Distinct()
            .ToArray();
        _collectors.Clear();
        _currentConsumer = null;

        return Task.WhenAll(consumers.Select(static consumer => consumer.ForceCompleteAsync()));
    }

    async Task<BatchConsumer<TMessage>> AddAsync(ConsumeContext<TMessage> context, Activity? currentActivity)
    {
        if (_keyProvider.TryGetKey(context, out var key))
        {
            if (_collectors.TryGetValue(key, out BatchConsumer<TMessage>? consumer))
            {
                if (context.Advanced().GetRetryAttempt() > 0)
                    await consumer.ForceCompleteAsync().ConfigureAwait(false);
            }

            if (consumer == null || consumer.IsCompleted)
            {
                consumer = new BatchConsumer<TMessage>(
                    _options,
                    _lifetime.Collector,
                    _lifetime.Dispatcher,
                    _consumerPipe,
                    context.GetTimeProvider());
                _collectors[key] = consumer;
            }

            await consumer.AddAsync(context, currentActivity).ConfigureAwait(false);

            return consumer;
        }

        if (_currentConsumer != null)
        {
            if (context.Advanced().GetRetryAttempt() > 0)
                await _currentConsumer.ForceCompleteAsync().ConfigureAwait(false);
        }

        if (_currentConsumer == null || _currentConsumer.IsCompleted)
        {
            _currentConsumer = new BatchConsumer<TMessage>(
                _options,
                _lifetime.Collector,
                _lifetime.Dispatcher,
                _consumerPipe,
                context.GetTimeProvider());
        }

        await _currentConsumer.AddAsync(context, currentActivity).ConfigureAwait(false);

        return _currentConsumer;
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
