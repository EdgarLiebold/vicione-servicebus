using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for query saga repository.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface IQuerySagaRepository<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>
    /// Performs the find operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default);
}
