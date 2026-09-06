using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Stores and retrieves query saga data.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IQuerySagaRepository<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>Finds the matching value.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the matching value.</returns>
    Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default);
}
