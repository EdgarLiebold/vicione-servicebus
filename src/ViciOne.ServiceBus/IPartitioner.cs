using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface IPartitioner :
    IAsyncDisposable,
    IProbeSite
{
    IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
        where T : class, PipeContext;
}


public interface IPartitioner<TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>
    /// Sends the context through the partitioner
    /// </summary>
    /// <param name="context">The context</param>
    /// <param name="next">The next pipe</param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync(TContext context, IPipe<TContext> next, CancellationToken cancellationToken = default);
}
