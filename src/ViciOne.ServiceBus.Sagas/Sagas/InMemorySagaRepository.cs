using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Stores saga state by reference and exposes message, load and query repository capabilities.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public class InMemorySagaRepository<TSaga> :
    ISagaRepository<TSaga>,
    IQuerySagaRepository<TSaga>,
    ILoadSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly LoadSagaRepository<TSaga> _loadRepository;
    readonly QuerySagaRepository<TSaga> _queryRepository;
    readonly SagaRepository<TSaga> _repository;
    readonly IndexedSagaDictionary<TSaga> _sagas;

    /// <summary>Creates an empty dictionary shared by the message, load and query repository operations.</summary>
    public InMemorySagaRepository()
    {
        _sagas = new IndexedSagaDictionary<TSaga>();

        var factory = new InMemorySagaConsumeContextFactory<TSaga>();

        var repositoryContextFactory = new InMemorySagaRepositoryContextFactory<TSaga>(_sagas, factory);

        _repository = new SagaRepository<TSaga>(repositoryContextFactory);
        _queryRepository = new QuerySagaRepository<TSaga>(repositoryContextFactory);
        _loadRepository = new LoadSagaRepository<TSaga>(repositoryContextFactory);
    }

    /// <summary>Gets the retained saga wrapper for an identifier, or null when no wrapper is stored.</summary>
    /// <param name="id">The saga correlation identifier.</param>
    public SagaInstance<TSaga>? this[Guid id] => _sagas[id];

    /// <summary>Gets the number of correlation-identifier entries in the saga dictionary.</summary>
    public int Count => _sagas.Count;

    /// <summary>Loads live saga state through the dictionary's load capability without acquiring a saga lease.</summary>
    /// <param name="correlationId">The identifier of the requested saga.</param>
    /// <param name="cancellationToken">The token that cancels the load operation.</param>
    /// <returns>The retained state by reference, or null when the saga is absent or invalidated.</returns>
    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _loadRepository.LoadAsync(correlationId, cancellationToken);
    }

    /// <summary>Finds matching saga identifiers through the dictionary's query capability.</summary>
    /// <param name="query">The predicate evaluated against the retained saga states.</param>
    /// <param name="cancellationToken">The token that cancels the query operation.</param>
    /// <returns>The matching identifiers; the saga states themselves are not copied.</returns>
    public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        return _queryRepository.FindAsync(query, cancellationToken);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("sagaRepository");
        scope.Set(new
        {
            _sagas.Count,
            Persistence = "memory"
        });
    }

    Task ISagaRepository<TSaga>.SendAsync<T>(ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
    {
        return _repository.SendAsync(context, policy, next);
    }

    Task ISagaRepository<TSaga>.SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, ISagaPolicy<TSaga, T> policy,
        IPipe<SagaConsumeContext<TSaga, T>> next)
    {
        return _repository.SendQueryAsync(context, query, policy, next);
    }
}
