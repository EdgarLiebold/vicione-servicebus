using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>The modern query saga repository, which can be used with any storage engine. Leverages the new interfaces for query context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class QuerySagaRepository<TSaga> :
    IQuerySagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly IQuerySagaRepositoryContextFactory<TSaga> _repositoryContextFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repositoryContextFactory">The repository context factory.</param>
    public QuerySagaRepository(IQuerySagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory;
    }

    /// <summary>Finds the matching value.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the matching value.</returns>
    public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        return _repositoryContextFactory.ExecuteAsync<IEnumerable<Guid>>(async context => await context.QueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("querySagaRepository");

        _repositoryContextFactory.Probe(scope);
    }
}
