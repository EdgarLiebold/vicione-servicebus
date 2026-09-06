using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Exposes state for query saga repository operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface QuerySagaRepositoryContext<TSaga> :
    PipeContext
    where TSaga : class, ISaga
{
    /// <summary>Query saga instances.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the query outcome.</returns>
    Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default);
}
