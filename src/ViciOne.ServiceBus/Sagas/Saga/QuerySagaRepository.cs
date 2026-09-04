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

    public QuerySagaRepository(IQuerySagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory;
    }

    public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        return _repositoryContextFactory.ExecuteAsync<IEnumerable<Guid>>(async context => await context.QueryAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("querySagaRepository");

        _repositoryContextFactory.Probe(scope);
    }
}
