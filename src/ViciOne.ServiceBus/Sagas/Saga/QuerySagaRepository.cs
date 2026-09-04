using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// The modern query saga repository, which can be used with any storage engine. Leverages the new interfaces for query context.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public class QuerySagaRepository<TSaga> :
    IQuerySagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly IQuerySagaRepositoryContextFactory<TSaga> _repositoryContextFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repositoryContextFactory">The repository context factory value.</param>
    public QuerySagaRepository(IQuerySagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory;
    }

    /// <summary>
    /// Performs the find operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        return _repositoryContextFactory.ExecuteAsync<IEnumerable<Guid>>(async context => await context.QueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("querySagaRepository");

        _repositoryContextFactory.Probe(scope);
    }
}
