using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a partition filter implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class PartitionFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IPartitioner<TContext> _partitioner;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="keyProvider">The key provider value.</param>
    /// <param name="partitioner">The partitioner value.</param>
    public PartitionFilter(PartitionKeyProvider<TContext> keyProvider, IPartitioner partitioner)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentNullException.ThrowIfNull(partitioner);

        _partitioner = partitioner.GetPartitioner(keyProvider);
    }

    Task IFilter<TContext>.SendAsync(TContext context, IPipe<TContext> next)
    {
        return _partitioner.SendAsync(context, next);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("partition");
        _partitioner.Probe(scope);
    }
}
