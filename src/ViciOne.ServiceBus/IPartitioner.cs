using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by partitioner.</summary>
public interface IPartitioner :
    IAsyncDisposable,
    IProbeSite
{
    /// <summary>Gets partitioner.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="keyProvider">The key provider.</param>
    /// <returns>The partitioner.</returns>
    IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
        where T : class, PipeContext;
}


/// <summary>Defines the operations required by partitioner.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IPartitioner<TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>Sends the context through the partitioner.</summary>
    /// <param name="context">The context.</param>
    /// <param name="next">The next pipe.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(TContext context, IPipe<TContext> next, CancellationToken cancellationToken = default);
}
