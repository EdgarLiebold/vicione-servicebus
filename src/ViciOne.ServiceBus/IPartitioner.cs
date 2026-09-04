using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for partitioner.
/// </summary>
public interface IPartitioner :
    IAsyncDisposable,
    IProbeSite
{
    /// <summary>
    /// Gets partitioner.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="keyProvider">The key provider value.</param>
    /// <returns>The result of the operation.</returns>
    IPartitioner<T> GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
        where T : class, PipeContext;
}


/// <summary>
/// Defines the contract for partitioner.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
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
