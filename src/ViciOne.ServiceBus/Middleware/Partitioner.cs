using System;
using System.Linq;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a partitioner implementation.
/// </summary>
public class Partitioner :
    IPartitioner
{
    readonly IHashGenerator _hashGenerator;
    readonly string _id;
    readonly int _partitionCount;
    readonly Partition[] _partitions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="partitionCount">The partition count value.</param>
    /// <param name="hashGenerator">The hash generator value.</param>
    public Partitioner(int partitionCount, IHashGenerator hashGenerator)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(partitionCount, 1);
        ArgumentNullException.ThrowIfNull(hashGenerator);

        _id = Guid.NewGuid().ToString("N");

        _partitionCount = partitionCount;
        _hashGenerator = hashGenerator;
        _partitions = Enumerable.Range(0, partitionCount)
            .Select(index => new Partition(index))
            .ToArray();
    }

    /// <summary>
    /// Gets partitioner.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="keyProvider">The key provider value.</param>
    /// <returns>The result of the operation.</returns>
    public IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
        where T : class, PipeContext
    {
        return new ContextPartitioner<T>(this, keyProvider);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("partitioner");
        scope.Add("id", _id);
        scope.Add("partitionCount", _partitionCount);

        foreach (var partition in _partitions)
            partition.Probe(scope);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        foreach (var partition in _partitions)
            await partition.DisposeAsync().ConfigureAwait(false);
    }

    Task SendAsync<T>(byte[] key, T context, IPipe<T> next)
        where T : class, PipeContext
    {
        var hash = key.Length > 0 ? _hashGenerator.Hash(key) : 0;

        var partitionId = hash % _partitionCount;

        return _partitions[partitionId].SendAsync(context, next);
    }


    class ContextPartitioner<TContext> :
        IPartitioner<TContext>
        where TContext : class, PipeContext
    {
        readonly PartitionKeyProvider<TContext> _keyProvider;
        readonly Partitioner _partitioner;

        public ContextPartitioner(Partitioner partitioner, PartitionKeyProvider<TContext> keyProvider)
        {
            _partitioner = partitioner ?? throw new ArgumentNullException(nameof(partitioner));
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        }

        public Task SendAsync(TContext context, IPipe<TContext> next, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); var key = _keyProvider(context);
            if (key == null)
                throw new InvalidOperationException("The partition key provider returned null.");

            return _partitioner.SendAsync(key, context, next);
        }

        public void Probe(ProbeContext context)
        {
            _partitioner.Probe(context);
        }
    }
}
