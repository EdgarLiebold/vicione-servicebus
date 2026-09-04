using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides an in memory saga repository context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class InMemorySagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaRepositoryContext<TSaga, TMessage>,
    IDisposable
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> _factory;
    readonly IndexedSagaDictionary<TSaga> _sagas;
    bool _sagasLocked;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sagas">The sagas value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="context">The operation context.</param>
    public InMemorySagaRepositoryContext(IndexedSagaDictionary<TSaga> sagas, ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> factory,
        ConsumeContext<TMessage> context)
        : base(context)
    {
        _sagas = sagas;
        _factory = factory;
        _context = context;
        _sagasLocked = true;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_sagasLocked)
        {
            _sagas.Release();
            _sagasLocked = false;
        }
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_sagasLocked)
        {
            SagaConsumeContext<TSaga, TMessage> consumeContext =
                await _factory.CreateSagaConsumeContextAsync(_sagas, _context, instance, SagaConsumeContextMode.Add).ConfigureAwait(false);

            _sagas.Release();
            _sagasLocked = false;

            return consumeContext;
        }

        await _sagas.MarkInUseAsync(_context.CancellationToken).ConfigureAwait(false);
        try
        {
            return await _factory.CreateSagaConsumeContextAsync(_sagas, _context, instance, SagaConsumeContextMode.Add).ConfigureAwait(false);
        }
        finally
        {
            _sagas.Release();
        }
    }

    /// <summary>
    /// Performs the insert operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_sagasLocked)
        {
            if (_sagas[instance.CorrelationId] != null)
                return default;

            SagaConsumeContext<TSaga, TMessage> consumeContext =
                await _factory.CreateSagaConsumeContextAsync(_sagas, _context, instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);

            _sagas.Release();
            _sagasLocked = false;

            return consumeContext;
        }

        await _sagas.MarkInUseAsync(_context.CancellationToken).ConfigureAwait(false);
        try
        {
            if (_sagas[instance.CorrelationId] != null)
                return default;

            return await _factory.CreateSagaConsumeContextAsync(_sagas, _context, instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);
        }
        finally
        {
            _sagas.Release();
        }
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); SagaInstance<TSaga>? saga;
        if (_sagasLocked)
        {
            saga = _sagas[correlationId];
            if (saga == null)
                return default;

            if (saga.IsRemoved)
            {
                saga.Release();
                return default;
            }

            _sagas.Release();
            _sagasLocked = false;
        }
        else
        {
            await _sagas.MarkInUseAsync(_context.CancellationToken).ConfigureAwait(false);
            try
            {
                saga = _sagas[correlationId];

                if (saga == null)
                    return default;

                if (saga.IsRemoved)
                {
                    saga.Release();
                    return default;
                }
            }
            finally
            {
                _sagas.Release();
            }
        }

        return await _factory.CreateSagaConsumeContextAsync(_sagas, _context, saga.Instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the save operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return SaveAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the delete operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _sagas.MarkInUseAsync(CancellationToken).ConfigureAwait(false);
        try
        {
            SagaInstance<TSaga> instance = _sagas[context.Saga.CorrelationId]
                ?? throw new InvalidOperationException($"Saga {context.Saga.CorrelationId} was not found in the in-memory repository.");

            _sagas.Remove(instance);
        }
        finally
        {
            _sagas.Release();
        }
    }

    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the undo operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
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
        return _factory.CreateSagaConsumeContextAsync(_sagas, consumeContext, instance, mode);
    }
}


/// <summary>
/// Provides an in memory saga repository context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class InMemorySagaRepositoryContext<TSaga> :
    BasePipeContext,
    QuerySagaRepositoryContext<TSaga>,
    LoadSagaRepositoryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IndexedSagaDictionary<TSaga> _sagas;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sagas">The sagas value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public InMemorySagaRepositoryContext(IndexedSagaDictionary<TSaga> sagas, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _sagas = sagas;
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _sagas.MarkInUseAsync(CancellationToken).ConfigureAwait(false);
        try
        {
            SagaInstance<TSaga>? saga = _sagas[correlationId];
            if (saga == null)
                return default;

            if (saga.IsRemoved)
            {
                saga.Release();
                return default;
            }

            return saga.Instance;
        }
        finally
        {
            _sagas.Release();
        }
    }

    /// <summary>
    /// Performs the query operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); List<Guid> matchingInstances = _sagas.Where(query).Select(x => x.Instance.CorrelationId).ToList();

        return new DefaultSagaRepositoryQueryContext<TSaga>(this, matchingInstances);
    }
}
