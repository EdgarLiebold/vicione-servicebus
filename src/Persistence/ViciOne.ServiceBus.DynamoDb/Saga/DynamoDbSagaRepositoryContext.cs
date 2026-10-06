using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>Applies versioned saga repository operations to Amazon DynamoDB within a message consume context.</summary>
/// <typeparam name="TSaga">The versioned saga state managed by the repository.</typeparam>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal sealed class DynamoDbSagaRepositoryContext<TSaga, TMessage>(
    IDynamoDbSagaStore<TSaga> store,
    ConsumeContext<TMessage> consumeContext,
    ISagaConsumeContextFactory<IDynamoDbSagaStore<TSaga>, TSaga> consumeContextFactory) :
    ConsumeContextScope<TMessage>(consumeContext ?? throw new ArgumentNullException(nameof(consumeContext))),
    ISagaRepositoryContext<TSaga, TMessage>,
    IDisposable
    where TSaga : class, ISagaVersion
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext = consumeContext;
    readonly IDynamoDbSagaStore<TSaga> _store = store ?? throw new ArgumentNullException(nameof(store));
    readonly ISagaConsumeContextFactory<IDynamoDbSagaStore<TSaga>, TSaga> _consumeContextFactory =
        consumeContextFactory ?? throw new ArgumentNullException(nameof(consumeContextFactory));

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _store.Dispose();
    }

    /// <summary>Wraps a new saga instance in an add-mode consume context without persisting it.</summary>
    /// <param name="instance">The new saga instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the add-mode saga consume context.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        cancellationToken.ThrowIfCancellationRequested();

        return _consumeContextFactory.CreateSagaConsumeContextAsync(
            _store,
            _consumeContext,
            instance,
            SagaConsumeContextMode.Add);
    }

    /// <summary>Conditionally inserts a saga document and returns its insert-mode consume context.</summary>
    /// <param name="instance">The saga instance to insert.</param>
    /// <param name="cancellationToken">The token checked before insertion; Amazon DynamoDB I/O uses the surrounding consume-context token.</param>
    /// <returns>A task whose result is the insert-mode saga consume context.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _store.CreateAsync(instance, _consumeContext.CancellationToken).ConfigureAwait(false);

            try
            {
                _consumeContext.LogInsert<TSaga, TMessage>(instance.CorrelationId);
            }
            catch (Exception)
            {
            }

            return await _consumeContextFactory.CreateSagaConsumeContextAsync(
                    _store,
                    _consumeContext,
                    instance,
                    SagaConsumeContextMode.Insert)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            try
            {
                _consumeContext.LogInsertFault<TSaga, TMessage>(ex, instance.CorrelationId);
            }
            catch (Exception)
            {
            }

            throw;
        }
    }

    /// <summary>Loads and validates a saga document by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token checked before loading; Amazon DynamoDB I/O uses the surrounding consume-context token.</param>
    /// <returns>A task whose result is the load-mode saga context, or <see langword="null"/> when no document exists.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TSaga? instance = await _store
            .LoadAsync(correlationId, _consumeContext.CancellationToken)
            .ConfigureAwait(false);
        if (instance == null)
            return null;

        return await _consumeContextFactory.CreateSagaConsumeContextAsync(
                _store,
                _consumeContext,
                instance,
                SagaConsumeContextMode.Load)
            .ConfigureAwait(false);
    }

    /// <summary>Conditionally persists the new saga from a consume context.</summary>
    /// <param name="context">The saga consume context whose instance is persisted.</param>
    /// <param name="cancellationToken">The token checked before persistence; Amazon DynamoDB I/O uses the token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional put.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return _store.CreateAsync(context.Saga, context.CancellationToken);
    }

    /// <summary>Updates the saga only when the persisted version still matches.</summary>
    /// <param name="context">The saga consume context whose instance is updated.</param>
    /// <param name="cancellationToken">The token checked before the update; Amazon DynamoDB I/O uses the token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional update.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return _store.UpdateAsync(context.Saga, context.CancellationToken);
    }

    /// <summary>Deletes the saga only when the persisted version still matches.</summary>
    /// <param name="context">The saga consume context whose instance is deleted.</param>
    /// <param name="cancellationToken">The token checked before deletion; Amazon DynamoDB I/O uses the token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional delete.</returns>
    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return _store.DeleteAsync(context.Saga, context.CancellationToken);
    }

    /// <summary>Completes an uncommitted discard without writing to Amazon DynamoDB.</summary>
    /// <param name="context">The saga context being discarded.</param>
    /// <param name="cancellationToken">The token checked before completing the no-op.</param>
    /// <returns>An already-completed task unless cancellation was requested.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    /// <summary>Completes an undo request without compensating Amazon DynamoDB state.</summary>
    /// <param name="context">The saga context whose operation is being undone.</param>
    /// <param name="cancellationToken">The token checked before completing the no-op.</param>
    /// <returns>An already-completed task unless cancellation was requested.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    /// <summary>Wraps a saga instance in a consume context for another message type.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="consumeContext">The message consume context to wrap.</param>
    /// <param name="instance">The saga instance associated with the message.</param>
    /// <param name="mode">The repository operation mode represented by the new context.</param>
    /// <returns>A task whose result is the saga consume context.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        ArgumentNullException.ThrowIfNull(instance);

        return _consumeContextFactory.CreateSagaConsumeContextAsync(_store, consumeContext, instance, mode);
    }
}


/// <summary>Provides load-only Amazon DynamoDB access outside a message consume context.</summary>
/// <typeparam name="TSaga">The versioned saga state loaded by the repository.</typeparam>
internal sealed class DynamoDbSagaLoadContext<TSaga>(
    IDynamoDbSagaStore<TSaga> store,
    CancellationToken cancellationToken) :
    BasePipeContext(cancellationToken),
    ILoadSagaRepositoryContext<TSaga>,
    IDisposable
    where TSaga : class, ISagaVersion
{
    readonly IDynamoDbSagaStore<TSaga> _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _store.Dispose();
    }

    /// <summary>Loads and validates a saga document by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token checked before loading; Amazon DynamoDB I/O uses the load-context lifetime token.</param>
    /// <returns>A task whose result is the saga state, or <see langword="null"/> when no document exists.</returns>
    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _store.LoadAsync(correlationId, CancellationToken);
    }
}
