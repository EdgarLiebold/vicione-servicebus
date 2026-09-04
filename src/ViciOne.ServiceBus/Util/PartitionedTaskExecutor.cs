using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

#nullable enable
namespace ViciOne.ServiceBus.Util;

public sealed class PartitionedTaskExecutor<T> :
    IPartitionedTaskExecutor<T>
{
    readonly IHashGenerator _hashGenerator;
    readonly object _lifecycleLock = new();
    readonly PartitionKeyProvider<T> _partitionKeyProvider;
    readonly Lazy<TaskExecutor>[] _partitions;
    Task? _disposeTask;

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

    public Task EnqueueAsync(T partition, Func<Task> method, CancellationToken cancellationToken = default)
    {
        return GetExecutor(partition).EnqueueAsync(method, cancellationToken);
    }

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
