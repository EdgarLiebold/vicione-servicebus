using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Creates typed views that route complete pipeline operations through shared partitions.</summary>
public interface IPartitioner :
    IProbeSite
{
    /// <summary>Creates a typed view that derives a stable, non-null partition key from each context.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="keyProvider">Extracts the binary partition key.</param>
    /// <returns>The typed partitioner.</returns>
    IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
        where T : class, PipeContext;
}

/// <summary>Serializes pipeline execution within the partition selected for each context.</summary>
/// <typeparam name="TContext">The pipeline context type.</typeparam>
public interface IPartitioner<TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>Waits for the selected partition and then invokes the downstream pipeline.</summary>
    /// <param name="context">The context to schedule.</param>
    /// <param name="next">The pipeline stage to invoke within the selected partition.</param>
    /// <param name="cancellationToken">An additional token that cancels only the partition-admission wait.</param>
    /// <returns>A task that completes after the downstream stage finishes within the selected partition.</returns>
    Task SendAsync(TContext context, IPipe<TContext> next, CancellationToken cancellationToken = default);
}
