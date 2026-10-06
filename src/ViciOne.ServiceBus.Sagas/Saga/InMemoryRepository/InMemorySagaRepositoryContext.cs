using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Coordinates message-specific saga operations and owns the initial dictionary lease.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public class InMemorySagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    ISagaRepositoryContext<TSaga, TMessage>,
    IDisposable
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly object _dictionaryLeaseLock = new();
    readonly ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> _factory;
    readonly IndexedSagaDictionary<TSaga> _sagas;
    bool _sagasLocked;
    int _initialDictionaryUsers;
    bool _initialDictionaryReleaseRequested;

    /// <summary>Takes ownership of a dictionary lease already acquired by the caller.</summary>
    /// <param name="sagas">The required dictionary whose acquired lease is transferred to this context.</param>
    /// <param name="factory">The required factory that creates message contexts and owns their saga acquisition.</param>
    /// <param name="context">The required consumed-message context.</param>
    public InMemorySagaRepositoryContext(IndexedSagaDictionary<TSaga> sagas, ISagaConsumeContextFactory<IndexedSagaDictionary<TSaga>, TSaga> factory,
        ConsumeContext<TMessage> context)
        : base(context)
    {
        _sagas = sagas ?? throw new ArgumentNullException(nameof(sagas));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _context = context;
        _sagasLocked = true;
    }

    /// <summary>Requests once-only release of the initial dictionary lease after its active operations finish.</summary>
    /// <remarks>Does not wait synchronously for operations or release a lease while an operation still uses it.</remarks>
    public void Dispose()
    {
        ReleaseInitialDictionaryLease();
    }

    /// <summary>Creates an Add-mode saga context while holding the dictionary lease.</summary>
    /// <param name="instance">The required saga state supplied to the factory.</param>
    /// <param name="cancellationToken">The operation token, or the message token when this token cannot be cancelled.</param>
    /// <returns>The factory's consume context; successful creation requests dictionary-lease release after all active initial-lease operations finish.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        if (TryRetainInitialDictionaryLease())
        {
            bool releaseLease = false;
            try
            {
                SagaConsumeContext<TSaga, TMessage> consumeContext =
                    await _factory.CreateSagaConsumeContextAsync(_sagas, GetOperationConsumeContext(operationCancellationToken, cancellationToken), instance, SagaConsumeContextMode.Add).ConfigureAwait(false);
                releaseLease = true;
                return consumeContext;
            }
            finally
            {
                CompleteInitialDictionaryOperation(releaseLease);
            }
        }

        await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
        try
        {
            return await _factory.CreateSagaConsumeContextAsync(_sagas, GetOperationConsumeContext(operationCancellationToken, cancellationToken), instance, SagaConsumeContextMode.Add).ConfigureAwait(false);
        }
        finally
        {
            _sagas.Release();
        }
    }

    /// <summary>Creates an Insert-mode context only when no saga occupies the supplied identifier.</summary>
    /// <param name="instance">The required saga state supplied to the factory.</param>
    /// <param name="cancellationToken">The operation token, or the message token when this token cannot be cancelled.</param>
    /// <returns>The new consume context, or null for an occupied identifier without releasing an initial dictionary lease.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        if (TryRetainInitialDictionaryLease())
        {
            bool releaseLease = false;
            try
            {
                if (_sagas[instance.CorrelationId] != null)
                    return default;

                SagaConsumeContext<TSaga, TMessage> consumeContext =
                    await _factory.CreateSagaConsumeContextAsync(_sagas, GetOperationConsumeContext(operationCancellationToken, cancellationToken), instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);
                releaseLease = true;
                return consumeContext;
            }
            finally
            {
                CompleteInitialDictionaryOperation(releaseLease);
            }
        }

        await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
        try
        {
            if (_sagas[instance.CorrelationId] != null)
                return default;

            return await _factory.CreateSagaConsumeContextAsync(_sagas, GetOperationConsumeContext(operationCancellationToken, cancellationToken), instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);
        }
        finally
        {
            _sagas.Release();
        }
    }

    /// <summary>Finds a live saga and asks the factory to acquire its message consume context.</summary>
    /// <param name="correlationId">The identifier of the requested saga.</param>
    /// <param name="cancellationToken">The operation token, or the message token when this token cannot be cancelled.</param>
    /// <returns>The acquired consume context, or null when the saga is absent or already invalidated.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        SagaInstance<TSaga>? saga;
        if (TryRetainInitialDictionaryLease())
        {
            bool releaseLease = false;
            try
            {
                saga = _sagas[correlationId];
                if (saga == null || saga.IsRemoved)
                    return default;

                releaseLease = true;
            }
            finally
            {
                CompleteInitialDictionaryOperation(releaseLease);
            }
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
                    return default;
            }
            finally
            {
                _sagas.Release();
            }
        }

        ConsumeContext<TMessage> operationContext = cancellationToken.CanBeCanceled
            && operationCancellationToken != _context.CancellationToken
                ? new OperationConsumeContext(_context, operationCancellationToken)
                : _context;

        while (true)
        {
            try
            {
                return await _factory.CreateSagaConsumeContextAsync(_sagas, operationContext, saga.Instance, SagaConsumeContextMode.Load)
                    .ConfigureAwait(false);
            }
            catch (SagaInstanceRemovedException)
            {
                await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
                try
                {
                    saga = _sagas[correlationId];
                    if (saga == null || saga.IsRemoved)
                        return default;
                }
                finally
                {
                    _sagas.Release();
                }
            }
        }
    }

    /// <summary>Acknowledges state already held by reference in the in-memory repository, checking operation cancellation.</summary>
    /// <param name="context">The required consume context whose state is already retained by reference.</param>
    /// <param name="cancellationToken">The operation token, or the message token when this token cannot be cancelled.</param>
    /// <returns>A completed acknowledgement or a task cancelled with the selected token.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        return operationCancellationToken.IsCancellationRequested
            ? Task.FromCanceled(operationCancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Acknowledges the current in-memory state through the same cancellation check as saving.</summary>
    /// <param name="context">The required consume context whose state is already retained by reference.</param>
    /// <param name="cancellationToken">The operation token, or the message token when this token cannot be cancelled.</param>
    /// <returns>The save acknowledgement without copying or restoring state.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return SaveAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Removes the saga only when the dictionary still retains the context's exact state.</summary>
    /// <param name="context">The required consume context selecting the state to remove.</param>
    /// <param name="cancellationToken">The operation token, or the message token when this token cannot be cancelled.</param>
    /// <returns>A task that removes and invalidates the retained saga, or fails without deleting a replacement; the entered scope requests lease release on either outcome.</returns>
    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        bool initialLease = TryRetainInitialDictionaryLease();
        if (!initialLease)
            await _sagas.MarkInUseAsync(operationCancellationToken).ConfigureAwait(false);
        try
        {
            SagaInstance<TSaga> instance = _sagas[context.Saga.CorrelationId]
                ?? throw new InvalidOperationException($"Saga {context.Saga.CorrelationId} was not found in the in-memory repository.");

            if (!ReferenceEquals(instance.Instance, context.Saga))
                throw new InvalidOperationException($"Saga {context.Saga.CorrelationId} was replaced in the in-memory repository.");

            _sagas.Remove(instance);
        }
        finally
        {
            if (initialLease)
                CompleteInitialDictionaryOperation(releaseLease: true);
            else
                _sagas.Release();
        }
    }

    /// <summary>Discards retained state through the same identity-checked removal as deleting.</summary>
    /// <param name="context">The required consume context selecting the state to remove.</param>
    /// <param name="cancellationToken">The operation token, or the message token when this token cannot be cancelled.</param>
    /// <returns>The deletion task; a stale context cannot discard a replacement saga.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Checks operation cancellation without restoring earlier values of the referenced saga instance.</summary>
    /// <param name="context">The required consume context whose referenced state is left unchanged.</param>
    /// <param name="cancellationToken">The operation token, or the message token when this token cannot be cancelled.</param>
    /// <returns>A completed acknowledgement or a task cancelled with the selected token.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        return operationCancellationToken.IsCancellationRequested
            ? Task.FromCanceled(operationCancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>Delegates consume-context creation to this repository's configured factory.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="consumeContext">The message context supplied to the factory.</param>
    /// <param name="instance">The saga state supplied to the factory.</param>
    /// <param name="mode">The operation mode supplied to the factory.</param>
    /// <returns>The configured factory's consume-context task.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _factory.CreateSagaConsumeContextAsync(_sagas, consumeContext, instance, mode);
    }

    ConsumeContext<TMessage> GetOperationConsumeContext(CancellationToken operationCancellationToken, CancellationToken cancellationToken) =>
        cancellationToken.CanBeCanceled && operationCancellationToken != _context.CancellationToken
            ? new OperationConsumeContext(_context, operationCancellationToken)
            : _context;

    sealed class OperationConsumeContext : ConsumeContextProxy<TMessage>
    {
        readonly CancellationToken _operationCancellationToken;

        public OperationConsumeContext(ConsumeContext<TMessage> context, CancellationToken cancellationToken)
            : base(context)
        {
            _operationCancellationToken = cancellationToken;
        }

        public override CancellationToken CancellationToken => _operationCancellationToken;
    }

    void ReleaseInitialDictionaryLease()
    {
        lock (_dictionaryLeaseLock)
        {
            _initialDictionaryReleaseRequested = true;
            ReleaseInitialDictionaryLeaseIfUnused();
        }
    }

    bool TryRetainInitialDictionaryLease()
    {
        lock (_dictionaryLeaseLock)
        {
            if (!_sagasLocked)
                return false;

            _initialDictionaryUsers++;
            return true;
        }
    }

    void CompleteInitialDictionaryOperation(bool releaseLease)
    {
        lock (_dictionaryLeaseLock)
        {
            _initialDictionaryReleaseRequested |= releaseLease;
            _initialDictionaryUsers--;
            ReleaseInitialDictionaryLeaseIfUnused();
        }
    }

    void ReleaseInitialDictionaryLeaseIfUnused()
    {
        if (_sagasLocked && _initialDictionaryReleaseRequested && _initialDictionaryUsers == 0)
        {
            _sagasLocked = false;
            _sagas.Release();
        }
    }

    CancellationToken GetOperationCancellationToken(CancellationToken cancellationToken) =>
        cancellationToken.CanBeCanceled ? cancellationToken : _context.CancellationToken;
}


/// <summary>Loads referenced saga state and produces matching identifiers without owning a saga lease.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public class InMemorySagaRepositoryContext<TSaga> :
    BasePipeContext,
    IQuerySagaRepositoryContext<TSaga>,
    ILoadSagaRepositoryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly IndexedSagaDictionary<TSaga> _sagas;

    /// <summary>Retains the required dictionary and the callback's default cancellation token.</summary>
    /// <param name="sagas">The required saga dictionary.</param>
    /// <param name="cancellationToken">The token used when an operation supplies no cancellable token.</param>
    public InMemorySagaRepositoryContext(IndexedSagaDictionary<TSaga> sagas, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _sagas = sagas ?? throw new ArgumentNullException(nameof(sagas));
    }

    /// <summary>Returns a live saga by reference under a temporary dictionary lease.</summary>
    /// <param name="correlationId">The identifier of the requested saga.</param>
    /// <param name="cancellationToken">The operation token, or this context's token when it cannot be cancelled.</param>
    /// <returns>The retained state, or null for an absent or invalidated saga; no saga lease is acquired.</returns>
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
                return default;

            return saga.Instance;
        }
        finally
        {
            _sagas.Release();
        }
    }

    /// <summary>Evaluates the supplied query and materializes the original registered identifiers of its matching membership snapshot.</summary>
    /// <param name="query">The required saga predicate evaluated by the dictionary outside owner locks.</param>
    /// <param name="cancellationToken">The operation token, or this context's token when it cannot be cancelled.</param>
    /// <returns>A repository query context carrying the captured identifiers and this context's payloads, even if a callback retires or replaces a snapshot member.</returns>
    public async Task<ISagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        CancellationToken operationCancellationToken = GetOperationCancellationToken(cancellationToken);
        operationCancellationToken.ThrowIfCancellationRequested();

        List<Guid> matchingInstances = _sagas.GetMatchingCorrelationIds(query);

        return new DefaultSagaRepositoryQueryContext<TSaga>(this, matchingInstances);
    }

    CancellationToken GetOperationCancellationToken(CancellationToken cancellationToken) =>
        cancellationToken.CanBeCanceled ? cancellationToken : CancellationToken;
}
