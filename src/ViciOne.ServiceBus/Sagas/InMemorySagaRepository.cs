using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides an in memory saga repository implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class InMemorySagaRepository<TSaga> :
    ISagaRepository<TSaga>,
    IQuerySagaRepository<TSaga>,
    ILoadSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaRepository<TSaga> _repository;
    readonly IndexedSagaDictionary<TSaga> _sagas;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public InMemorySagaRepository()
    {
        _sagas = new IndexedSagaDictionary<TSaga>();

        var factory = new InMemorySagaConsumeContextFactory<TSaga>();

        var repositoryContextFactory = new InMemorySagaRepositoryContextFactory<TSaga>(_sagas, factory);

        _repository = new SagaRepository<TSaga>(repositoryContextFactory, repositoryContextFactory, repositoryContextFactory);
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="id">The id value.</param>
    public SagaInstance<TSaga>? this[Guid id] => _sagas[id];

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _sagas.Count;

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _repository.LoadAsync(correlationId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the find operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<IEnumerable<Guid>> FindAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        return _repository.FindAsync(query, cancellationToken: cancellationToken);
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
