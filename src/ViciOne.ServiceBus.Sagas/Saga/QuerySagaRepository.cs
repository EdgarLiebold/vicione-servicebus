using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Finds saga identifiers through a storage-specific query context.</summary>
/// <typeparam name="TSaga">The saga state queried by the repository.</typeparam>
public class QuerySagaRepository<TSaga> :
    IQuerySagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly IQuerySagaRepositoryContextFactory<TSaga> _repositoryContextFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repositoryContextFactory">The repository context factory.</param>
    public QuerySagaRepository(IQuerySagaRepositoryContextFactory<TSaga> repositoryContextFactory)
    {
        _repositoryContextFactory = repositoryContextFactory
            ?? throw new ArgumentNullException(nameof(repositoryContextFactory));
    }

    /// <summary>Finds the matching value.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the matching value.</returns>
    public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return _repositoryContextFactory.ExecuteAsync<IEnumerable<Guid>>(
            async context => await context.QueryAsync(query, cancellationToken).ConfigureAwait(false),
            cancellationToken);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("querySagaRepository");

        _repositoryContextFactory.Probe(scope);
    }
}
