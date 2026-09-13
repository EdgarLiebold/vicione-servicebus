using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Stores and retrieves in memory saga data.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
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

    /// <summary>Initializes a new instance.</summary>
    public InMemorySagaRepository()
    {
        _sagas = new IndexedSagaDictionary<TSaga>();

        var factory = new InMemorySagaConsumeContextFactory<TSaga>();

        var repositoryContextFactory = new InMemorySagaRepositoryContextFactory<TSaga>(_sagas, factory);

        _repository = new SagaRepository<TSaga>(repositoryContextFactory);
        _queryRepository = new QuerySagaRepository<TSaga>(repositoryContextFactory);
        _loadRepository = new LoadSagaRepository<TSaga>(repositoryContextFactory);
    }

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="id">The id.</param>
    public SagaInstance<TSaga>? this[Guid id] => _sagas[id];

    /// <summary>Gets the count.</summary>
    public int Count => _sagas.Count;

    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outcome.</returns>
    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _loadRepository.LoadAsync(correlationId, cancellationToken);
    }

    /// <summary>Finds the matching value.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the matching value.</returns>
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
