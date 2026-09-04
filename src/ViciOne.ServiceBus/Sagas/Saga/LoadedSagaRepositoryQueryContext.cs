using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// For queries that load the actual saga instances
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class LoadedSagaRepositoryQueryContext<TSaga, TMessage> :
    ConsumeContextProxy<TMessage>,
    SagaRepositoryQueryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IDictionary<Guid, TSaga> _index;
    readonly SagaRepositoryContext<TSaga, TMessage> _repositoryContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repositoryContext">The repository context value.</param>
    /// <param name="instances">The instances value.</param>
    public LoadedSagaRepositoryQueryContext(SagaRepositoryContext<TSaga, TMessage> repositoryContext, IEnumerable<TSaga> instances)
        : base(repositoryContext)
    {
        _repositoryContext = repositoryContext;

        _index = instances.ToDictionary(x => x.CorrelationId);
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _index.Count;

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.AddAsync(instance, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the insert operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.InsertAsync(instance, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (_index.TryGetValue(correlationId, out var instance))
            return await _repositoryContext.CreateSagaConsumeContextAsync(_repositoryContext, instance, SagaConsumeContextMode.Load)
                .ConfigureAwait(false);

        return await _repositoryContext.LoadAsync(correlationId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the save operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.SaveAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.DiscardAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the undo operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.UndoAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.UpdateAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the delete operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return _repositoryContext.DeleteAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<Guid> GetEnumerator()
    {
        return _index.Keys.GetEnumerator();
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
        return _repositoryContext.CreateSagaConsumeContextAsync(consumeContext, instance, mode);
    }
}


/// <summary>
/// For queries that load the actual saga instances
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public class LoadedSagaRepositoryQueryContext<TSaga> :
    BasePipeContext,
    SagaRepositoryQueryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IDictionary<Guid, TSaga> _index;
    readonly QuerySagaRepositoryContext<TSaga> _querySagaRepositoryContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="querySagaRepositoryContext">The query saga repository context value.</param>
    /// <param name="instances">The instances value.</param>
    public LoadedSagaRepositoryQueryContext(QuerySagaRepositoryContext<TSaga> querySagaRepositoryContext, IEnumerable<TSaga> instances)
        : base(querySagaRepositoryContext)
    {
        _querySagaRepositoryContext = querySagaRepositoryContext;

        _index = instances.ToDictionary(x => x.CorrelationId);
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _index.Count;

    /// <summary>
    /// Performs the query operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default)
    {
        return _querySagaRepositoryContext.QueryAsync(query, cancellationToken);
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<Guid> GetEnumerator()
    {
        return _index.Keys.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
