using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Carries state for default saga repository query operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class DefaultSagaRepositoryQueryContext<TSaga, TMessage> :
    ConsumeContextProxy<TMessage>,
    ISagaRepositoryQueryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ISagaRepositoryContext<TSaga, TMessage> _context;
    readonly IList<Guid> _results;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="results">The results.</param>
    public DefaultSagaRepositoryQueryContext(ISagaRepositoryContext<TSaga, TMessage> context, IList<Guid> results)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _context = context;
        _results = results ?? throw new ArgumentNullException(nameof(results));
    }

    /// <summary>Gets the count.</summary>
    public int Count => _results.Count;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the add outcome.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _context.AddAsync(instance, cancellationToken: cancellationToken);
    }

    /// <summary>Inserts the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the insert outcome.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _context.InsertAsync(instance, cancellationToken: cancellationToken);
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outcome.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        return _context.LoadAsync(correlationId, cancellationToken: cancellationToken);
    }

    /// <summary>Persists the current state.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.SaveAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Discards the current value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.DiscardAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Reverts the current operation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.UndoAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Updates the current value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.UpdateAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Deletes the selected entity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _context.DeleteAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Guid> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Creates saga consume context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">The consume context.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="mode">The mode.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _context.CreateSagaConsumeContextAsync(consumeContext, instance, mode);
    }
}


/// <summary>Carries state for default saga repository query operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DefaultSagaRepositoryQueryContext<TSaga> :
    ProxyPipeContext,
    ISagaRepositoryQueryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IQuerySagaRepositoryContext<TSaga> _queryContext;
    readonly IList<Guid> _results;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="queryContext">The query context.</param>
    /// <param name="results">The results.</param>
    public DefaultSagaRepositoryQueryContext(IQuerySagaRepositoryContext<TSaga> queryContext, IList<Guid> results)
        : base(queryContext ?? throw new ArgumentNullException(nameof(queryContext)))
    {
        _queryContext = queryContext;
        _results = results ?? throw new ArgumentNullException(nameof(results));
    }

    /// <summary>Gets the count.</summary>
    public int Count => _results.Count;

    /// <summary>Queries the configured data source.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the query outcome.</returns>
    public Task<ISagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        return _queryContext.QueryAsync(query, cancellationToken);
    }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Guid> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
