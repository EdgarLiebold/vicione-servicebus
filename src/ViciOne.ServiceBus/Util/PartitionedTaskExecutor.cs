using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides a partitioned task executor implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public sealed class PartitionedTaskExecutor<T> :
    IPartitionedTaskExecutor<T>
{
    readonly IHashGenerator _hashGenerator;
    readonly object _lifecycleLock = new();
    readonly PartitionKeyProvider<T> _partitionKeyProvider;
    readonly Lazy<TaskExecutor>[] _partitions;
    Task? _disposeTask;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="partitionKeyProvider">The partition key provider value.</param>
    /// <param name="hashGenerator">The hash generator value.</param>
    /// <param name="partitionCount">The partition count value.</param>
    /// <param name="concurrentDeliveryLimit">The concurrent delivery limit value.</param>
    /// <param name="partitionCapacity">The partition capacity value.</param>
    public PartitionedTaskExecutor(PartitionKeyProvider<T> partitionKeyProvider, IHashGenerator hashGenerator, int partitionCount,
        int concurrentDeliveryLimit = 1, int? partitionCapacity = null)
    {
        ArgumentNullException.ThrowIfNull(partitionKeyProvider);
        ArgumentNullException.ThrowIfNull(hashGenerator);

        if (partitionCount < 1)
            throw new ArgumentOutOfRangeException(nameof(partitionCount), partitionCount, "Must be >= 1");
        if (concurrentDeliveryLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrentDeliveryLimit), concurrentDeliveryLimit, "Must be >= 1");
        if (partitionCapacity is < 1)
            throw new ArgumentOutOfRangeException(nameof(partitionCapacity), partitionCapacity, "Must be >= 1");

        _partitionKeyProvider = partitionKeyProvider;
        _hashGenerator = hashGenerator;
        _partitions = Enumerable.Range(0, partitionCount)
            .Select(_ => new Lazy<TaskExecutor>(() => partitionCapacity.HasValue
                ? new TaskExecutor(partitionCapacity.Value, concurrentDeliveryLimit)
                : new TaskExecutor(concurrentDeliveryLimit), LazyThreadSafetyMode.ExecutionAndPublication))
            .ToArray();
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        lock (_lifecycleLock)
        {
            _disposeTask ??= DisposePartitionsAsync(_partitions
                .Where(partition => partition.IsValueCreated)
                .Select(partition => partition.Value)
                .ToArray());

            return new ValueTask(_disposeTask);
        }
    }

    /// <summary>
    /// Performs the enqueue operation.
    /// </summary>
    /// <param name="partition">The partition value.</param>
    /// <param name="method">The method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task EnqueueAsync(T partition, Func<Task> method, CancellationToken cancellationToken = default)
    {
        return GetExecutor(partition).EnqueueAsync(method, cancellationToken);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="partition">The partition value.</param>
    /// <param name="method">The method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(T partition, Func<Task> method, CancellationToken cancellationToken = default)
    {
        return GetExecutor(partition).ExecuteAsync(method, cancellationToken);
    }

    TaskExecutor GetExecutor(T partition)
    {
        int index;
        if (_partitions.Length == 1)
            index = 0;
        else
        {
            byte[]? partitionKey = _partitionKeyProvider(partition);
            uint hash = partitionKey is { Length: > 0 } ? _hashGenerator.Hash(partitionKey) : 0;
            index = (int)(hash % (uint)_partitions.Length);
        }

        lock (_lifecycleLock)
        {
            if (_disposeTask != null)
                throw new ObjectDisposedException(nameof(PartitionedTaskExecutor<T>));

            return _partitions[index].Value;
        }
    }

    static async Task DisposePartitionsAsync(TaskExecutor[] partitions)
    {
        foreach (TaskExecutor partition in partitions)
            await partition.DisposeAsync().ConfigureAwait(false);
    }
}
