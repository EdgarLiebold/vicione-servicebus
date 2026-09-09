using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware.Partitioning;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Maps pipeline contexts onto stable partitions and serializes execution within each partition.</summary>
public sealed class PipePartitioner :
    IPartitioner,
    IAsyncDisposable
{
    readonly PartitionCoordinator _coordinator;
    readonly object _lifetimeLock;
    int _acceptedOperationCount;
    TaskCompletionSource? _disposeCompletion;
    Task? _disposeTask;

    /// <summary>Creates a partitioner that uses the stable ViciOne MurmurHash3 algorithm.</summary>
    /// <param name="partitionCount">The number of independently serialized partitions.</param>
    public PipePartitioner(int partitionCount)
        : this(partitionCount, new Murmur3PartitionHashGenerator())
    {
    }

    /// <summary>Creates a partitioner with a custom stable hashing algorithm.</summary>
    /// <param name="partitionCount">The number of independently serialized partitions.</param>
    /// <param name="hashGenerator">The thread-safe algorithm used to select a partition from a key.</param>
    public PipePartitioner(int partitionCount, IPartitionHashGenerator hashGenerator)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);
        ArgumentNullException.ThrowIfNull(hashGenerator);

        _coordinator = new PartitionCoordinator(partitionCount, hashGenerator);
        _lifetimeLock = new object();
    }

    /// <summary>Creates a typed view that derives a partition key from each context.</summary>
    /// <typeparam name="T">The pipeline context type processed by the partitioner.</typeparam>
    /// <param name="keyProvider">The function that selects the partition key for a context.</param>
    /// <returns>A context-specific view of this partitioner.</returns>
    public IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(keyProvider);

        lock (_lifetimeLock)
        {
            ObjectDisposedException.ThrowIf(_disposeTask != null, this);
            return new OwnedContextPartitioner<T>(this, keyProvider);
        }
    }

    /// <summary>Reports this partitioner's identity, partition count, and per-partition outcomes.</summary>
    /// <param name="context">The probe that receives the partitioner state.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _coordinator.Probe(context);
    }

    /// <summary>Stops accepting contexts and drains every context accepted before disposal began.</summary>
    /// <returns>A task that completes when every partition has drained.</returns>
    public ValueTask DisposeAsync()
    {
        lock (_lifetimeLock)
        {
            if (_disposeTask != null)
                return new ValueTask(_disposeTask);

            _disposeCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (_acceptedOperationCount == 0)
                _disposeCompletion.TrySetResult();

            _disposeTask = DisposeCoreAsync(_disposeCompletion.Task);
            return new ValueTask(_disposeTask);
        }
    }

    async Task DisposeCoreAsync(Task drained)
    {
        await drained.ConfigureAwait(false);
        _coordinator.Dispose();
    }

    async Task SendAsync<T>(
        PartitionKeyProvider<T> keyProvider,
        T context,
        IPipe<T> next,
        CancellationToken cancellationToken)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        BeginOperation();
        try
        {
            byte[] key = keyProvider(context)
                ?? throw new InvalidOperationException("The partition key provider returned null.");
            await _coordinator.SendAsync(key, context, next, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            CompleteOperation();
        }
    }

    void BeginOperation()
    {
        lock (_lifetimeLock)
        {
            ObjectDisposedException.ThrowIf(_disposeTask != null, this);
            _acceptedOperationCount++;
        }
    }

    void CompleteOperation()
    {
        TaskCompletionSource? disposeCompletion = null;
        lock (_lifetimeLock)
        {
            _acceptedOperationCount--;
            if (_acceptedOperationCount == 0)
                disposeCompletion = _disposeCompletion;
        }

        disposeCompletion?.TrySetResult();
    }

    private sealed class OwnedContextPartitioner<TContext> :
        IPartitioner<TContext>
        where TContext : class, PipeContext
    {
        readonly PartitionKeyProvider<TContext> _keyProvider;
        readonly PipePartitioner _partitioner;

        public OwnedContextPartitioner(PipePartitioner partitioner, PartitionKeyProvider<TContext> keyProvider)
        {
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        }

        public Task SendAsync(TContext context, IPipe<TContext> next, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            return _partitioner.SendAsync(_keyProvider, context, next, cancellationToken);
        }

        public void Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _partitioner.Probe(context);
        }
    }
}
