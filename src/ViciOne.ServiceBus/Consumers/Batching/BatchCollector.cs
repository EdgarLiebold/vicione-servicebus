using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Batching;

public class BatchCollector<TMessage> :
    IBatchCollector<TMessage>
    where TMessage : class
{
    readonly TaskExecutor _collector;
    readonly IPipe<ConsumeContext<Batch<TMessage>>> _consumerPipe;
    readonly TaskExecutor _dispatcher;
    readonly BatchOptions _options;
    BatchConsumer<TMessage>? _currentConsumer;

    public BatchCollector(BatchOptions options, IPipe<ConsumeContext<Batch<TMessage>>> consumerPipe)
    {
        _options = options;
        _consumerPipe = consumerPipe;

        _collector = new TaskExecutor();
        _dispatcher = new TaskExecutor(options.ConcurrencyLimit);
    }

    public async ValueTask DisposeAsync()
    {
        await _collector.DisposeAsync().ConfigureAwait(false);
        await _dispatcher.DisposeAsync().ConfigureAwait(false);
    }

    public Task<BatchConsumer<TMessage>> CollectAsync(ConsumeContext<TMessage> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Batching.BatchConsumer<TMessage>>(cancellationToken); var currentActivity = Activity.Current;

        return _collector.ExecuteAsync(() => AddAsync(context, currentActivity), context.CancellationToken);
    }

    public Task CompleteAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer, CancellationToken cancellationToken = default)
    {
        return _collector.ExecuteAsync(() => RemoveAsync(consumer), cancellationToken: cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("batchCollector");

        _consumerPipe.Probe(scope);
    }

    Task RemoveAsync(BatchConsumer<TMessage> consumer)
    {
        if (_currentConsumer == consumer)
            _currentConsumer = null;

        return Task.CompletedTask;
    }

    async Task<BatchConsumer<TMessage>> AddAsync(ConsumeContext<TMessage> context, Activity? currentActivity)
    {
        if (_currentConsumer != null)
        {
            if (context.Advanced().GetRetryAttempt() > 0)
                await _currentConsumer.ForceCompleteAsync().ConfigureAwait(false);
        }

        if (_currentConsumer == null || _currentConsumer.IsCompleted)
            _currentConsumer = new BatchConsumer<TMessage>(_options, _collector, _dispatcher, _consumerPipe, context.GetTimeProvider());

        await _currentConsumer.AddAsync(context, currentActivity).ConfigureAwait(false);

        return _currentConsumer;
    }
}


public class BatchCollector<TMessage, TKey> :
    IBatchCollector<TMessage>
    where TMessage : class
    where TKey : notnull
{
    readonly TaskExecutor _collector;
    readonly IDictionary<TKey, BatchConsumer<TMessage>> _collectors;
    readonly IPipe<ConsumeContext<Batch<TMessage>>> _consumerPipe;
    readonly TaskExecutor _dispatcher;
    readonly IGroupKeyProvider<TMessage, TKey> _keyProvider;
    readonly BatchOptions _options;
    BatchConsumer<TMessage>? _currentConsumer;

    public BatchCollector(BatchOptions options, IPipe<ConsumeContext<Batch<TMessage>>> consumerPipe, IGroupKeyProvider<TMessage, TKey> keyProvider)
    {
        _options = options;
        _consumerPipe = consumerPipe;
        _keyProvider = keyProvider;

        _collector = new TaskExecutor();
        _dispatcher = new TaskExecutor(options.ConcurrencyLimit);
        _collectors = new Dictionary<TKey, BatchConsumer<TMessage>>();
    }

    public async ValueTask DisposeAsync()
    {
        await _collector.DisposeAsync().ConfigureAwait(false);
        await _dispatcher.DisposeAsync().ConfigureAwait(false);
    }

    public Task<BatchConsumer<TMessage>> CollectAsync(ConsumeContext<TMessage> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Batching.BatchConsumer<TMessage>>(cancellationToken); var currentActivity = Activity.Current;

        return _collector.ExecuteAsync(() => AddAsync(context, currentActivity), context.CancellationToken);
    }

    public Task CompleteAsync(ConsumeContext<TMessage> context, BatchConsumer<TMessage> consumer, CancellationToken cancellationToken = default)
    {
        return _collector.ExecuteAsync(() => RemoveAsync(context, consumer), cancellationToken: cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
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
                consumer = new BatchConsumer<TMessage>(_options, _collector, _dispatcher, _consumerPipe, context.GetTimeProvider());
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
            _currentConsumer = new BatchConsumer<TMessage>(_options, _collector, _dispatcher, _consumerPipe, context.GetTimeProvider());

        await _currentConsumer.AddAsync(context, currentActivity).ConfigureAwait(false);

        return _currentConsumer;
    }
}
