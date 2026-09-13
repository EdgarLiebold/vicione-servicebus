using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates saga repository context instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ISagaRepositoryContextFactory<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>Create a <see cref="ISagaRepositoryContext{TSaga,T}" /> and send it to the next pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync<T>(ConsumeContext<T> context, IPipe<ISagaRepositoryContext<TSaga, T>> next)
        where T : class;

    /// <summary>Create a <see cref="ISagaRepositoryQueryContext{TSaga,T}" /> and send it to the next pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="query">The query.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<ISagaRepositoryQueryContext<TSaga, T>> next)
        where T : class;
}
