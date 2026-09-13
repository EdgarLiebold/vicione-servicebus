using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Executes storage-specific saga queries within an open repository operation.</summary>
/// <typeparam name="TSaga">The persisted saga state type.</typeparam>
public interface IQuerySagaRepositoryContext<TSaga> :
    PipeContext
    where TSaga : class, ISaga
{
    /// <summary>Evaluates a saga query and returns the matching correlation identifiers.</summary>
    /// <param name="query">The storage-independent saga predicate.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that returns the query result context.</returns>
    Task<ISagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default);
}
