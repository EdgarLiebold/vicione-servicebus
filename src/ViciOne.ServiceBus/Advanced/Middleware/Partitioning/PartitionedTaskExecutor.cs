using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Maps values onto stable bounded worker queues. Each partition preserves admission order and uses
/// its own worker pool; execution and completion remain serial only when that pool has one worker.
/// </summary>
/// <typeparam name="T">The value from which a worker-partition key is derived.</typeparam>
public sealed class PartitionedTaskExecutor<T> :
    IPartitionedTaskExecutor<T>
{
    readonly IPartitionHashGenerator _hashGenerator;
    readonly object _lifecycleLock = new();
    readonly PartitionKeyProvider<T> _partitionKeyProvider;
    readonly Lazy<TaskExecutor>[] _partitions;
    Task? _disposeTask;

    /// <summary>Creates lazily allocated worker partitions with explicit concurrency and optional capacity limits.</summary>
    /// <param name="partitionKeyProvider">Extracts the bytes used to select a stable worker partition.</param>
    /// <param name="partitionCount">The number of independently scheduled worker partitions.</param>
    /// <param name="concurrencyLimit">The maximum concurrent delegates within each worker partition; one preserves serial execution.</param>
    /// <param name="partitionCapacity">The optional number of delegates that may wait within each worker partition.</param>
    /// <param name="hashGenerator">The optional algorithm used to hash partition keys.</param>
    public PartitionedTaskExecutor(PartitionKeyProvider<T> partitionKeyProvider, int partitionCount,
        int concurrencyLimit = 1, int? partitionCapacity = null, IPartitionHashGenerator? hashGenerator = null)
    {
        ArgumentNullException.ThrowIfNull(partitionKeyProvider);

        if (partitionCount < 1)
            throw new ArgumentOutOfRangeException(nameof(partitionCount), partitionCount, "Must be >= 1");
        if (concurrencyLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrencyLimit), concurrencyLimit, "Must be >= 1");
        if (partitionCapacity is < 1)
            throw new ArgumentOutOfRangeException(nameof(partitionCapacity), partitionCapacity, "Must be >= 1");

        _partitionKeyProvider = partitionKeyProvider;
        _hashGenerator = hashGenerator ?? new Murmur3PartitionHashGenerator();
        _partitions = new Lazy<TaskExecutor>[partitionCount];
        for (var index = 0; index < partitionCount; index++)
        {
            _partitions[index] = new Lazy<TaskExecutor>(() => partitionCapacity.HasValue
                ? new TaskExecutor(partitionCapacity.Value, concurrencyLimit)
                : new TaskExecutor(concurrencyLimit), LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }

    /// <summary>Stops new admissions and drains every worker partition that was activated.</summary>
    /// <returns>A task that completes after all activated partitions stop.</returns>
    public ValueTask DisposeAsync()
    {
        lock (_lifecycleLock)
        {
            if (_disposeTask == null)
            {
                var activatedPartitions = new List<TaskExecutor>(_partitions.Length);
                foreach (Lazy<TaskExecutor> partition in _partitions)
                {
                    if (partition.IsValueCreated)
                        activatedPartitions.Add(partition.Value);
                }

                _disposeTask = DisposePartitionsAsync(activatedPartitions);
            }

            return new ValueTask(_disposeTask);
        }
    }

    /// <summary>Transfers a delegate to the bounded queue selected by its partition value.</summary>
    /// <param name="partition">The value used to select a stable worker partition.</param>
    /// <param name="method">The delegate whose execution ownership transfers to the selected worker.</param>
    /// <param name="cancellationToken">Cancels only the queue-admission wait.</param>
    /// <returns>A task that completes when the selected queue accepts the delegate.</returns>
    public Task EnqueueAsync(T partition, Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        return GetExecutor(partition).EnqueueAsync(method, cancellationToken);
    }

    /// <summary>Runs a delegate on the queue selected by its partition value and waits for completion.</summary>
    /// <param name="partition">The value used to select a stable worker partition.</param>
    /// <param name="method">The delegate to execute.</param>
    /// <param name="cancellationToken">Cancels queue admission or execution before the delegate begins.</param>
    /// <returns>A task that completes with the delegate.</returns>
    public Task ExecuteAsync(T partition, Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        return GetExecutor(partition).ExecuteAsync(method, cancellationToken);
    }

    TaskExecutor GetExecutor(T partition)
    {
        EnsureAccepting();

        byte[] partitionKey = _partitionKeyProvider(partition)
            ?? throw new InvalidOperationException("The partition key provider returned null.");
        uint hash = _partitions.Length > 1
            ? _hashGenerator.ComputeHash(partitionKey)
            : 0;
        int index = (int)(hash % (uint)_partitions.Length);

        lock (_lifecycleLock)
        {
            if (_disposeTask != null)
                throw new ObjectDisposedException(nameof(PartitionedTaskExecutor<T>));

            return _partitions[index].Value;
        }
    }

    void EnsureAccepting()
    {
        lock (_lifecycleLock)
        {
            if (_disposeTask != null)
                throw new ObjectDisposedException(nameof(PartitionedTaskExecutor<T>));
        }
    }

    static async Task DisposePartitionsAsync(IReadOnlyList<TaskExecutor> partitions)
    {
        var disposals = new Task[partitions.Count];
        for (var index = 0; index < partitions.Count; index++)
            disposals[index] = partitions[index].DisposeAsync().AsTask();

        await Task.WhenAll(disposals).ConfigureAwait(false);
    }
}
