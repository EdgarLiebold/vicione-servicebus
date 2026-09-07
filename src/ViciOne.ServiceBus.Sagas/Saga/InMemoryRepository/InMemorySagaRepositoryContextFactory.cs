using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Supports the InMemorySagaRepository.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class InMemorySagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>,
    IQuerySagaRepositoryContextFactory<TSaga>,
    ILoadSagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> _factory;
    readonly IndexedSagaDictionary<TSaga> _sagas;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sagas">The sagas.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    public InMemorySagaRepositoryContextFactory(IndexedSagaDictionary<TSaga> sagas, ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> factory)
    {
        _sagas = sagas;
        _factory = factory;
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="asyncMethod">The async method.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the execute outcome.</returns>
    public Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        return ExecuteAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="asyncMethod">The async method.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the execute outcome.</returns>
    public Task<T> ExecuteAsync<T>(Func<QuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
        where T : class
    {
        return ExecuteAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("count", _sagas.Count);
        context.Add("persistence", "memory");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        await _sagas.MarkInUseAsync(context.CancellationToken).ConfigureAwait(false);

        using var repositoryContext = new InMemorySagaRepositoryContext<TSaga, T>(_sagas, _factory, context);

        await next.SendAsync(repositoryContext).ConfigureAwait(false);
    }

    /// <summary>Sends query.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="query">The query.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
