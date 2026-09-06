using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.DynamoDb.Saga;

/// <summary>Applies versioned saga repository operations to Amazon DynamoDB within a message consume context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class DynamoDbSagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaRepositoryContext<TSaga, TMessage>,
    IDisposable
    where TSaga : class, ISagaVersion
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext;
    readonly DatabaseContext<TSaga> _context;
    readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;

    /// <summary>Creates a repository context for one consumed message.</summary>
    /// <param name="context">The Amazon DynamoDB persistence context for the saga type.</param>
    /// <param name="consumeContext">The message consume context that owns cancellation and logging.</param>
    /// <param name="factory">The factory that wraps saga instances in consume contexts.</param>
    public DynamoDbSagaRepositoryContext(DatabaseContext<TSaga> context, ConsumeContext<TMessage> consumeContext,
        ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory)
        : base(consumeContext)
    {
        _context = context;
        _consumeContext = consumeContext;
        _factory = factory;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>Wraps a new saga instance in an add-mode consume context without persisting it.</summary>
    /// <param name="instance">The new saga instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the add-mode saga consume context.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

    /// <summary>Conditionally inserts a saga document and returns its insert-mode consume context.</summary>
    /// <param name="instance">The saga instance to insert.</param>
    /// <param name="cancellationToken">The token checked before insertion; Amazon DynamoDB I/O uses the surrounding consume-context token.</param>
    /// <returns>A task whose result is the insert-mode saga consume context.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); try
        {
            await _context.InsertAsync(instance, _consumeContext.CancellationToken).ConfigureAwait(false);

            _consumeContext.LogInsert<TSaga, TMessage>(instance.CorrelationId);

            return await _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _consumeContext.LogInsertFault<TSaga, TMessage>(ex, instance.CorrelationId);

            throw;
        }
    }

    /// <summary>Loads and validates a saga document by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token checked before loading; Amazon DynamoDB I/O uses the surrounding consume-context token.</param>
    /// <returns>A task whose result is the load-mode saga context, or <see langword="null"/> when no document exists.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instance = await _context.LoadAsync(correlationId, _consumeContext.CancellationToken).ConfigureAwait(false);
        if (instance == null)
            return default;

        return await _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
    }

    /// <summary>Conditionally persists the new saga from a consume context.</summary>
    /// <param name="context">The saga consume context whose instance is persisted.</param>
    /// <param name="cancellationToken">The token checked before persistence; Amazon DynamoDB I/O uses the token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional put.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _context.AddAsync(context.Saga, context.CancellationToken);
    }

    /// <summary>Updates the saga only when the persisted version still matches.</summary>
    /// <param name="context">The saga consume context whose instance is updated.</param>
    /// <param name="cancellationToken">The token checked before the update; Amazon DynamoDB I/O uses the token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional update.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _context.UpdateAsync(context.Saga, context.CancellationToken);
    }

    /// <summary>Deletes the saga only when the persisted version still matches.</summary>
    /// <param name="context">The saga consume context whose instance is deleted.</param>
    /// <param name="cancellationToken">The token checked before deletion; Amazon DynamoDB I/O uses the token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Amazon DynamoDB accepts the conditional delete.</returns>
    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _context.DeleteAsync(context.Saga, context.CancellationToken);
    }

    /// <summary>Completes an uncommitted discard without writing to Amazon DynamoDB.</summary>
    /// <param name="context">The saga context being discarded.</param>
    /// <param name="cancellationToken">The token checked before completing the no-op.</param>
    /// <returns>An already-completed task unless cancellation was requested.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return TaskResults.Completed;
    }

    /// <summary>Completes an undo request without compensating Amazon DynamoDB state.</summary>
    /// <param name="context">The saga context whose operation is being undone.</param>
    /// <param name="cancellationToken">The token checked before completing the no-op.</param>
    /// <returns>An already-completed task unless cancellation was requested.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return TaskResults.Completed;
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
        return _factory.CreateSagaConsumeContextAsync(_context, consumeContext, instance, mode);
    }
}


/// <summary>Provides load-only Amazon DynamoDB access outside a message consume context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DynamoDbSagaRepositoryContext<TSaga> :
    BasePipeContext,
    LoadSagaRepositoryContext<TSaga>,
    IDisposable
    where TSaga : class, ISagaVersion
{
    readonly DatabaseContext<TSaga> _context;

    /// <summary>Creates a load context with its operation lifetime token.</summary>
    /// <param name="context">The Amazon DynamoDB persistence context for the saga type.</param>
    /// <param name="cancellationToken">The token that owns the load-context lifetime.</param>
    public DynamoDbSagaRepositoryContext(DatabaseContext<TSaga> context, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _context = context;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _context.Dispose();
    }

    /// <summary>Loads and validates a saga document by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token checked before loading; Amazon DynamoDB I/O uses the load-context lifetime token.</param>
    /// <returns>A task whose result is the saga state, or <see langword="null"/> when no document exists.</returns>
    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TSaga?>(cancellationToken); return _context.LoadAsync(correlationId, CancellationToken);
    }
}
