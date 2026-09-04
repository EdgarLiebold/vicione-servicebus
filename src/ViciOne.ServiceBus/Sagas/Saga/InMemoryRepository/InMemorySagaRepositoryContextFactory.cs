using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Supports the InMemorySagaRepository
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public class InMemorySagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>,
    IQuerySagaRepositoryContextFactory<TSaga>,
    ILoadSagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> _factory;
    readonly IndexedSagaDictionary<TSaga> _sagas;

    public InMemorySagaRepositoryContextFactory(IndexedSagaDictionary<TSaga> sagas, ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> factory)
    {
        _sagas = sagas;
        _factory = factory;
    }

    public Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        return ExecuteAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    public Task<T> ExecuteAsync<T>(Func<QuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
        where T : class
    {
        return ExecuteAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
        context.Add("count", _sagas.Count);
        context.Add("persistence", "memory");
    }

    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        await _sagas.MarkInUseAsync(context.CancellationToken).ConfigureAwait(false);

        using var repositoryContext = new InMemorySagaRepositoryContext<TSaga, T>(_sagas, _factory, context);

        await next.SendAsync(repositoryContext).ConfigureAwait(false);
    }

    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        await _sagas.MarkInUseAsync(context.CancellationToken).ConfigureAwait(false);

        using var repositoryContext = new InMemorySagaRepositoryContext<TSaga, T>(_sagas, _factory, context);

        List<Guid> matchingInstances = _sagas.Where(query).Select(x => x.Instance.CorrelationId).ToList();

        var queryContext = new DefaultSagaRepositoryQueryContext<TSaga, T>(repositoryContext, matchingInstances);

        await next.SendAsync(queryContext).ConfigureAwait(false);
    }

    Task<T> ExecuteAsyncMethodAsync<T>(Func<InMemorySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
    {
        var repositoryContext = new InMemorySagaRepositoryContext<TSaga>(_sagas, cancellationToken);

        return asyncMethod(repositoryContext);
    }
}
