using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public class PartitionFilter<TContext> :
    IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IPartitioner<TContext> _partitioner;

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
