using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>For queries that load the actual saga instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class LoadedSagaRepositoryQueryContext<TSaga, TMessage> :
    ConsumeContextProxy<TMessage>,
    ISagaRepositoryQueryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IDictionary<Guid, TSaga> _index;
    readonly ISagaRepositoryContext<TSaga, TMessage> _repositoryContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repositoryContext">The repository context.</param>
    /// <param name="instances">The instances.</param>
    public LoadedSagaRepositoryQueryContext(ISagaRepositoryContext<TSaga, TMessage> repositoryContext, IEnumerable<TSaga> instances)
        : base(repositoryContext)
    {
        _repositoryContext = repositoryContext;

        _index = instances.ToDictionary(x => x.CorrelationId);
    }

    /// <summary>Gets the count.</summary>
    public int Count => _index.Count;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the add outcome.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.AddAsync(instance, cancellationToken: cancellationToken);
    }

    /// <summary>Inserts the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the insert outcome.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.InsertAsync(instance, cancellationToken: cancellationToken);
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outcome.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (_index.TryGetValue(correlationId, out var instance))
            return await _repositoryContext.CreateSagaConsumeContextAsync(_repositoryContext, instance, SagaConsumeContextMode.Load)
                .ConfigureAwait(false);

        return await _repositoryContext.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Persists the current state.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.SaveAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Discards the current value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.DiscardAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Reverts the current operation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.UndoAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Updates the current value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.UpdateAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Deletes the selected entity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.DeleteAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Guid> GetEnumerator()
    {
        return _index.Keys.GetEnumerator();
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
        return _repositoryContext.CreateSagaConsumeContextAsync(consumeContext, instance, mode);
    }
}


/// <summary>For queries that load the actual saga instances.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class LoadedSagaRepositoryQueryContext<TSaga> :
    BasePipeContext,
    ISagaRepositoryQueryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IDictionary<Guid, TSaga> _index;
    readonly IQuerySagaRepositoryContext<TSaga> _querySagaRepositoryContext;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="querySagaRepositoryContext">The query saga repository context.</param>
    /// <param name="instances">The instances.</param>
    public LoadedSagaRepositoryQueryContext(IQuerySagaRepositoryContext<TSaga> querySagaRepositoryContext, IEnumerable<TSaga> instances)
        : base(querySagaRepositoryContext)
    {
        _querySagaRepositoryContext = querySagaRepositoryContext;

        _index = instances.ToDictionary(x => x.CorrelationId);
    }

    /// <summary>Gets the count.</summary>
    public int Count => _index.Count;

    /// <summary>Queries the configured data source.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the query outcome.</returns>
    public Task<ISagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        return _querySagaRepositoryContext.QueryAsync(query, cancellationToken);
    }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<Guid> GetEnumerator()
    {
        return _index.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
