using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Carries state for in memory saga repository operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class InMemorySagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    ISagaRepositoryContext<TSaga, TMessage>,
    IDisposable
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> _factory;
    readonly IndexedSagaDictionary<TSaga> _sagas;
    bool _sagasLocked;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sagas">The sagas.</param>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="context">The context associated with the operation.</param>
    public InMemorySagaRepositoryContext(IndexedSagaDictionary<TSaga> sagas, ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> factory,
        ConsumeContext<TMessage> context)
        : base(context)
    {
        _sagas = sagas;
        _factory = factory;
        _context = context;
        _sagasLocked = true;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        if (_sagasLocked)
        {
            _sagas.Release();
            _sagasLocked = false;
        }
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the add outcome.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        if (_sagasLocked)
        {
            SagaConsumeContext<TSaga, TMessage> consumeContext =
                await _factory.CreateSagaConsumeContextAsync(_sagas, _context, instance, SagaConsumeContextMode.Add).ConfigureAwait(false);

            _sagas.Release();
            _sagasLocked = false;

            return consumeContext;
        }

        await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
        try
        {
            return await _factory.CreateSagaConsumeContextAsync(_sagas, _context, instance, SagaConsumeContextMode.Add).ConfigureAwait(false);
        }
        finally
        {
            _sagas.Release();
        }
    }

    /// <summary>Inserts the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the insert outcome.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        if (_sagasLocked)
        {
            if (_sagas[instance.CorrelationId] != null)
                return default;

            SagaConsumeContext<TSaga, TMessage> consumeContext =
                await _factory.CreateSagaConsumeContextAsync(_sagas, _context, instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);

            _sagas.Release();
            _sagasLocked = false;

            return consumeContext;
        }

        await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
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

    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outcome.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        SagaInstance<TSaga>? saga;
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
            await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
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

        while (true)
        {
            try
            {
                return await _factory.CreateSagaConsumeContextAsync(_sagas, _context, saga.Instance, SagaConsumeContextMode.Load)
                    .ConfigureAwait(false);
            }
            catch (SagaInstanceRemovedException)
            {
                await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
                try
                {
                    saga = _sagas[correlationId];
                    if (saga == null)
                        return default;
                }
                finally
                {
                    _sagas.Release();
                }
            }
        }
    }

    /// <summary>Persists the current state.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        return operationCancellationToken.IsCancellationRequested
            ? Task.FromCanceled(operationCancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Updates the current value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return SaveAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Deletes the selected entity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
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

    /// <summary>Discards the current value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Reverts the current operation.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        return operationCancellationToken.IsCancellationRequested
            ? Task.FromCanceled(operationCancellationToken)
            : Task.CompletedTask;
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
        return _factory.CreateSagaConsumeContextAsync(_sagas, consumeContext, instance, mode);
    }

    CancellationToken GetOperationCancellationToken(CancellationToken cancellationToken) =>
        cancellationToken.CanBeCanceled ? cancellationToken : _context.CancellationToken;
}


/// <summary>Carries state for in memory saga repository operations.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class InMemorySagaRepositoryContext<TSaga> :
    BasePipeContext,
    IQuerySagaRepositoryContext<TSaga>,
    ILoadSagaRepositoryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IndexedSagaDictionary<TSaga> _sagas;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sagas">The sagas.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public InMemorySagaRepositoryContext(IndexedSagaDictionary<TSaga> sagas, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _sagas = sagas;
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outcome.</returns>
    public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
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

    /// <summary>Queries the configured data source.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the query outcome.</returns>
    public async Task<ISagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        List<Guid> matchingInstances = _sagas.Where(query).Select(x => x.Instance.CorrelationId).ToList();

        return new DefaultSagaRepositoryQueryContext<TSaga>(this, matchingInstances);
    }

    CancellationToken GetOperationCancellationToken(CancellationToken cancellationToken) =>
        cancellationToken.CanBeCanceled ? cancellationToken : CancellationToken;
}
