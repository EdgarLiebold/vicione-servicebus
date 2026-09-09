using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Partitioning;

/// <summary>Routes pipeline operations through the serialized partition selected from their key.</summary>
/// <typeparam name="TContext">The context type carried by the pipeline.</typeparam>
internal sealed class PartitionFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IPartitioner<TContext> _partitioner;

    /// <summary>Creates a filter for one context type and partition-key policy.</summary>
    /// <param name="keyProvider">The function that selects a partition key from each context.</param>
    /// <param name="partitioner">The partition owner shared by the configured pipelines.</param>
    public PartitionFilter(PartitionKeyProvider<TContext> keyProvider, IPartitioner partitioner)
    {
        ArgumentNullException.ThrowIfNull(keyProvider);
        ArgumentNullException.ThrowIfNull(partitioner);

        _partitioner = partitioner.GetPartitioner(keyProvider);
    }

    Task IFilter<TContext>.SendAsync(TContext context, IPipe<TContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return _partitioner.SendAsync(context, next);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("partition");
        _partitioner.Probe(scope);
    }
}
