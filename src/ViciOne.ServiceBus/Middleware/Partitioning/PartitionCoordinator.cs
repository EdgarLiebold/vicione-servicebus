using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Partitioning;

/// <summary>Routes complete pipeline operations through stable, independently serialized partitions.</summary>
internal sealed class PartitionCoordinator :
    IPartitioner,
    IDisposable
{
    readonly IPartitionHashGenerator _hashGenerator;
    readonly string _id;
    readonly Partition[] _partitions;

    /// <summary>Creates a coordinator that uses the stable ViciOne MurmurHash3 algorithm.</summary>
    /// <param name="partitionCount">The number of independently serialized partitions.</param>
    public PartitionCoordinator(int partitionCount)
        : this(partitionCount, new Murmur3PartitionHashGenerator())
    {
    }

    /// <summary>Creates a coordinator with a custom stable hashing algorithm.</summary>
    /// <param name="partitionCount">The number of independently serialized partitions.</param>
    /// <param name="hashGenerator">The thread-safe algorithm used to select a partition from a key.</param>
    public PartitionCoordinator(int partitionCount, IPartitionHashGenerator hashGenerator)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);
        _hashGenerator = hashGenerator ?? throw new ArgumentNullException(nameof(hashGenerator));

        _id = Guid.NewGuid().ToString("N");
        _partitions = new Partition[partitionCount];
        for (var index = 0; index < partitionCount; index++)
            _partitions[index] = new Partition(index);
    }

    /// <summary>Creates a typed view that derives a stable partition key from each context.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="keyProvider">The function that selects a non-null binary key.</param>
    /// <returns>A context-specific view of this coordinator.</returns>
    public IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(keyProvider);
        return new ContextPartitioner<T>(this, keyProvider);
    }

    /// <summary>Reports this coordinator's identity, partition count, and per-partition outcomes.</summary>
    /// <param name="context">The probe that receives the partition state.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("partitioner");
        scope.Add("id", _id);
        scope.Add("partitionCount", _partitions.Length);

        foreach (Partition partition in _partitions)
            partition.Probe(scope);
    }

    /// <summary>Releases partition semaphores after an external owner has drained all operations.</summary>
    public void Dispose()
    {
        foreach (Partition partition in _partitions)
            partition.Dispose();
    }

    internal Task SendAsync<T>(
        byte[] key,
        T context,
        IPipe<T> next,
        CancellationToken cancellationToken = default)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        uint hash = _hashGenerator.ComputeHash(key);
        int partitionIndex = (int)(hash % (uint)_partitions.Length);
        return _partitions[partitionIndex].SendAsync(context, next, cancellationToken);
    }

    private sealed class ContextPartitioner<TContext> :
        IPartitioner<TContext>
        where TContext : class, PipeContext
    {
        readonly PartitionCoordinator _coordinator;
        readonly PartitionKeyProvider<TContext> _keyProvider;

        public ContextPartitioner(PartitionCoordinator coordinator, PartitionKeyProvider<TContext> keyProvider)
        {
            _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        }

        public Task SendAsync(TContext context, IPipe<TContext> next, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            byte[] key = _keyProvider(context)
                ?? throw new InvalidOperationException("The partition key provider returned null.");
            return _coordinator.SendAsync(key, context, next, cancellationToken);
        }

        public void Probe(ProbeContext context)
        {
            _coordinator.Probe(context);
        }
    }
}
