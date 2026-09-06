using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates typed partitioners and owns their asynchronous lifetime.</summary>
public interface IPartitioner :
    IAsyncDisposable,
    IProbeSite
{
    /// <summary>Gets a partitioner that derives a stable partition key from each context.</summary>
    /// <typeparam name="T">The pipe-context type.</typeparam>
    /// <param name="keyProvider">Extracts the binary partition key.</param>
    /// <returns>The typed partitioner.</returns>
    IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
        where T : class, PipeContext;
}

/// <summary>Serializes pipeline execution within partitions selected for a context type.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IPartitioner<TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>Sends the context through the partitioner.</summary>
    /// <param name="context">The context to schedule.</param>
    /// <param name="next">The pipeline stage to invoke within the selected partition.</param>
    /// <param name="cancellationToken">Cancels admission to the partition.</param>
    /// <returns>A task that completes after the downstream stage finishes within the selected partition.</returns>
    Task SendAsync(TContext context, IPipe<TContext> next, CancellationToken cancellationToken = default);
}
