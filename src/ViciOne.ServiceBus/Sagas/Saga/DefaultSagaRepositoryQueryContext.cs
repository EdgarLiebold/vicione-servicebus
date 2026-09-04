using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides a default saga repository query context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class DefaultSagaRepositoryQueryContext<TSaga, TMessage> :
    ConsumeContextProxy<TMessage>,
    SagaRepositoryQueryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly SagaRepositoryContext<TSaga, TMessage> _context;
    readonly IList<Guid> _results;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="results">The results value.</param>
    public DefaultSagaRepositoryQueryContext(SagaRepositoryContext<TSaga, TMessage> context, IList<Guid> results)
        : base(context)
    {
        _context = context;
        _results = results;
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _results.Count;

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _context.AddAsync(instance, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the insert operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _context.InsertAsync(instance, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _context.LoadAsync(correlationId, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the save operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.SaveAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.DiscardAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the undo operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.UndoAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.UpdateAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the delete operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.DeleteAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<Guid> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Creates saga consume context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="instance">The instance value.</param>
    /// <param name="mode">The mode value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _context.CreateSagaConsumeContextAsync(consumeContext, instance, mode);
    }
}


/// <summary>
/// Provides a default saga repository query context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class DefaultSagaRepositoryQueryContext<TSaga> :
    ProxyPipeContext,
    SagaRepositoryQueryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly QuerySagaRepositoryContext<TSaga> _queryContext;
    readonly IList<Guid> _results;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queryContext">The query context value.</param>
    /// <param name="results">The results value.</param>
    public DefaultSagaRepositoryQueryContext(QuerySagaRepositoryContext<TSaga> queryContext, IList<Guid> results)
        : base(queryContext)
    {
        _queryContext = queryContext;
        _results = results;
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _results.Count;

    /// <summary>
    /// Performs the query operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        return _queryContext.QueryAsync(query, cancellationToken);
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<Guid> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
