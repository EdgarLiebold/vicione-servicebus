using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

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

    public InMemorySagaRepositoryContext(IndexedSagaDictionary<TSaga> sagas, ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> factory,
        ConsumeContext<TMessage> context)
        : base(context)
    {
        _sagas = sagas;
        _factory = factory;
        _context = context;
        _sagasLocked = true;
    }

    public void Dispose()
    {
        if (_sagasLocked)
        {
            _sagas.Release();
            _sagasLocked = false;
        }
    }

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

    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return SaveAsync(context, cancellationToken: cancellationToken);
    }

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

    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(context, cancellationToken: cancellationToken);
    }

    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _factory.CreateSagaConsumeContextAsync(_sagas, consumeContext, instance, mode);
    }
}


public class InMemorySagaRepositoryContext<TSaga> :
    BasePipeContext,
    QuerySagaRepositoryContext<TSaga>,
    LoadSagaRepositoryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IndexedSagaDictionary<TSaga> _sagas;

    public InMemorySagaRepositoryContext(IndexedSagaDictionary<TSaga> sagas, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _sagas = sagas;
    }

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

    public async Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); List<Guid> matchingInstances = _sagas.Where(query).Select(x => x.Instance.CorrelationId).ToList();

        return new DefaultSagaRepositoryQueryContext<TSaga>(this, matchingInstances);
    }
}
