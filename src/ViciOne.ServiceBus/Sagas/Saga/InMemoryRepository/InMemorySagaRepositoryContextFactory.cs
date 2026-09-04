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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sagas">The sagas value.</param>
    /// <param name="factory">The factory value.</param>
    public InMemorySagaRepositoryContextFactory(IndexedSagaDictionary<TSaga> sagas, ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> factory)
    {
        _sagas = sagas;
        _factory = factory;
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="asyncMethod">The async method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        return ExecuteAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="asyncMethod">The async method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<T> ExecuteAsync<T>(Func<QuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
        where T : class
    {
        return ExecuteAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("count", _sagas.Count);
        context.Add("persistence", "memory");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        await _sagas.MarkInUseAsync(context.CancellationToken).ConfigureAwait(false);

        using var repositoryContext = new InMemorySagaRepositoryContext<TSaga, T>(_sagas, _factory, context);

        await next.SendAsync(repositoryContext).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends query.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="query">The query value.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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
