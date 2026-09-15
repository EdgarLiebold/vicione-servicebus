using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Creates callback and message-operation contexts over one in-memory saga dictionary.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public class InMemorySagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>,
    IQuerySagaRepositoryContextFactory<TSaga>,
    ILoadSagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> _factory;
    readonly IndexedSagaDictionary<TSaga> _sagas;

    /// <summary>Retains the required dictionary and message consume-context factory.</summary>
    /// <param name="sagas">The required saga dictionary shared by all created contexts.</param>
    /// <param name="factory">The required factory that acquires message-specific saga contexts.</param>
    public InMemorySagaRepositoryContextFactory(IndexedSagaDictionary<TSaga> sagas, ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> factory)
    {
        _sagas = sagas ?? throw new ArgumentNullException(nameof(sagas));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>Invokes a load callback unless its supplied token is already cancelled.</summary>
    /// <typeparam name="T">The callback result type.</typeparam>
    /// <param name="asyncMethod">The required callback returning a non-null task; its result may be null.</param>
    /// <param name="cancellationToken">The token checked before invocation and carried by the created context.</param>
    /// <returns>The callback's task, or a task cancelled with the supplied token before invocation.</returns>
    public Task<T?> ExecuteAsync<T>(Func<ILoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        return ExecuteCallbackAsync(asyncMethod, cancellationToken);
    }

    /// <summary>Invokes a query callback unless its supplied token is already cancelled.</summary>
    /// <typeparam name="T">The callback result type.</typeparam>
    /// <param name="asyncMethod">The required callback returning a non-null task.</param>
    /// <param name="cancellationToken">The token checked before invocation and carried by the created context.</param>
    /// <returns>The callback's task, or a task cancelled with the supplied token before invocation.</returns>
    public Task<T> ExecuteAsync<T>(Func<IQuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
        where T : class
    {
        return ExecuteCallbackAsync(asyncMethod, cancellationToken);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe receiving the current saga count and memory persistence kind.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("count", _sagas.Count);
        context.Add("persistence", "memory");
    }

    /// <summary>Acquires the dictionary and invokes the message pipeline through an owning repository context.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The message context whose token cancels dictionary acquisition.</param>
    /// <param name="next">The pipeline receiving the message-specific repository context.</param>
    /// <returns>A task that completes after the pipeline returns and any remaining initial dictionary lease is disposed.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<ISagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        await _sagas.MarkInUseAsync(context.CancellationToken).ConfigureAwait(false);

        using var repositoryContext = new InMemorySagaRepositoryContext<TSaga, T>(_sagas, _factory, context);

        await next.SendAsync(repositoryContext).ConfigureAwait(false);
    }

    /// <summary>Materializes matching registered snapshot identifiers under the dictionary lease and invokes the query pipeline.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="context">The required message context whose token cancels dictionary acquisition.</param>
    /// <param name="query">The required saga predicate evaluated outside owner locks before invoking the pipeline.</param>
    /// <param name="next">The required pipeline receiving captured identifiers and repository operations; later state mutation does not change those identifiers.</param>
    /// <returns>A task that completes after the pipeline returns and any remaining initial dictionary lease is disposed.</returns>
    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<ISagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(next);
        await _sagas.MarkInUseAsync(context.CancellationToken).ConfigureAwait(false);

        using var repositoryContext = new InMemorySagaRepositoryContext<TSaga, T>(_sagas, _factory, context);

        List<Guid> matchingInstances = _sagas.GetMatchingCorrelationIds(query);

        var queryContext = new DefaultSagaRepositoryQueryContext<TSaga, T>(repositoryContext, matchingInstances);

        await next.SendAsync(queryContext).ConfigureAwait(false);
    }

    Task<T> ExecuteCallbackAsync<T>(Func<InMemorySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(asyncMethod);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<T>(cancellationToken);

        var repositoryContext = new InMemorySagaRepositoryContext<TSaga>(_sagas, cancellationToken);

        return asyncMethod(repositoryContext)
            ?? throw new InvalidOperationException("The saga repository callback returned a null task.");
    }
}
