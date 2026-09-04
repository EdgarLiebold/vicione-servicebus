using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus;

public class InMemorySagaRepository<TSaga> :
    ISagaRepository<TSaga>,
    IQuerySagaRepository<TSaga>,
    ILoadSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaRepository<TSaga> _repository;
    readonly IndexedSagaDictionary<TSaga> _sagas;

    public InMemorySagaRepository()
    {
        _sagas = new IndexedSagaDictionary<TSaga>();

        var factory = new InMemorySagaConsumeContextFactory<TSaga>();

        var repositoryContextFactory = new InMemorySagaRepositoryContextFactory<TSaga>(_sagas, factory);

        _repository = new SagaRepository<TSaga>(repositoryContextFactory, repositoryContextFactory, repositoryContextFactory);
    }

    public SagaInstance<TSaga>? this[Guid id] => _sagas[id];

    public int Count => _sagas.Count;

    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _repository.LoadAsync(correlationId, cancellationToken: cancellationToken);
    }

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
