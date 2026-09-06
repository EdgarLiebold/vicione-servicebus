using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Defines the contract for query saga repository context.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface QuerySagaRepositoryContext<TSaga> :
    PipeContext
    where TSaga : class, ISaga
{
    /// <summary>
    /// Query saga instances
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default);
}
