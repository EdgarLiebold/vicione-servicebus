using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates query saga repository context instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IQuerySagaRepositoryContextFactory<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>Create a <see cref="QuerySagaRepositoryContext{TSaga}" /> and send it to the next pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="asyncMethod">The async method.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the execute outcome.</returns>
    Task<T> ExecuteAsync<T>(Func<QuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class;
}
