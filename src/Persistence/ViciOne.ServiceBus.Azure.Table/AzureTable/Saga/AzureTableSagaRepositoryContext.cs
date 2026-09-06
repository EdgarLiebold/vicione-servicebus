using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.AzureTable.Saga;

/// <summary>Applies saga repository operations to Azure Table entities within a message consume context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class AzureTableSagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaRepositoryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext;
    readonly DatabaseContext<TSaga> _context;
    readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;

    /// <summary>Creates a repository context for one consumed message.</summary>
    /// <param name="context">The Azure Table access components for the saga type.</param>
    /// <param name="consumeContext">The message consume context that owns cancellation and logging.</param>
    /// <param name="factory">The factory that wraps saga instances in consume contexts.</param>
    public AzureTableSagaRepositoryContext(DatabaseContext<TSaga> context, ConsumeContext<TMessage> consumeContext,
        ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory)
        : base(RequireConsumeContext(consumeContext))
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);

        _context = context;
        _consumeContext = consumeContext;
        _factory = factory;
    }

    /// <summary>Wraps a new saga instance in an add-mode consume context without persisting it.</summary>
    /// <param name="instance">The new saga instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the add-mode saga consume context.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

    /// <summary>Inserts a saga entity and returns its insert-mode consume context.</summary>
    /// <param name="instance">The saga instance to insert.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the insert-mode context, or <see langword="null"/> when the entity already exists.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(instance);

        try
        {
            (Task<global::Azure.Response> insert, var entity) = TableInsert(instance);
            await insert.ConfigureAwait(false);
            _consumeContext.LogInsert<TSaga, TMessage>(instance.CorrelationId);

            return await CreateSagaConsumeContextAsync(entity, SagaConsumeContextMode.Insert).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            _consumeContext.LogInsertFault<TSaga, TMessage>(exception, instance.CorrelationId);
            return default;
        }
    }

    /// <summary>Loads a saga by its formatted correlation key and attaches its entity tag for later optimistic writes.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <param name="cancellationToken">The operation token required by the repository contract; Azure I/O currently uses the surrounding consume-context token.</param>
    /// <returns>A task whose result is the load-mode saga context, or <see langword="null"/> when no entity exists.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var (partitionKey, rowKey) = _context.Format(correlationId);

        NullableResponse<TableEntity> result = await _context.Table
            .GetEntityIfExistsAsync<TableEntity>(
                partitionKey,
                rowKey,
                cancellationToken: CancellationToken)
            .ConfigureAwait(false);

        if (result.HasValue)
            return await CreateSagaConsumeContextAsync(new TableEntity(result.Value), SagaConsumeContextMode.Load).ConfigureAwait(false);

        return default;
    }

    /// <summary>Inserts the saga from a consume context as a new Azure Table entity.</summary>
    /// <param name="context">The saga consume context whose instance is inserted.</param>
    /// <param name="cancellationToken">The token checked before the insert; Azure I/O uses the surrounding consume-context token.</param>
    /// <returns>A task that completes when Azure Table accepts the entity insertion.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); (Task<global::Azure.Response> insert, _) = TableInsert(context.Saga);
        return insert;
    }

    /// <summary>Replaces a persisted saga entity when its loaded entity tag still matches.</summary>
    /// <param name="context">The saga consume context containing the instance and loaded entity tag.</param>
    /// <param name="cancellationToken">The token checked before the update; Azure I/O uses the cancellation token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Azure Table commits the optimistic replacement.</returns>
    public async Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instance = context.Saga;

        try
        {
            var eTag = context.GetPayload<SagaETag>();
            IDictionary<string, object> dict = _context.Converter.GetDictionary(instance);
            var entity = new TableEntity(dict) { ETag = new ETag(eTag.ETag) };
            (entity.PartitionKey, entity.RowKey) = _context.Format(instance.CorrelationId);

            await _context.Table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, context.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            throw new ConcurrencyException("Saga update failed", typeof(TSaga), instance.CorrelationId, exception);
        }
        catch (Exception exception)
        {
            throw new SagaException("Saga update failed", typeof(TSaga), instance.CorrelationId, exception);
        }
    }

    /// <summary>Deletes a persisted saga entity when its loaded entity tag still matches.</summary>
    /// <param name="context">The saga consume context containing the instance and loaded entity tag.</param>
    /// <param name="cancellationToken">The token checked before deletion; Azure I/O uses the cancellation token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Azure Table commits the optimistic deletion.</returns>
    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instance = context.Saga;
        try
        {
            var (partitionKey, rowKey) = _context.Format(instance.CorrelationId);
            var eTag = context.GetPayload<SagaETag>();
            await _context.Table
                .DeleteEntityAsync(partitionKey, rowKey, new ETag(eTag.ETag), context.CancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RequestFailedException exception) when (exception.Status == 412)
        {
            throw new ConcurrencyException("Saga delete failed", typeof(TSaga), instance.CorrelationId, exception);
        }
        catch (Exception exception)
        {
            throw new SagaException("Saga delete failed", typeof(TSaga), instance.CorrelationId, exception);
        }
    }

    /// <summary>Completes an uncommitted discard without writing to Azure Table.</summary>
    /// <param name="context">The saga context being discarded.</param>
    /// <param name="cancellationToken">The token checked before completing the no-op.</param>
    /// <returns>An already-completed task unless cancellation was requested.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Completes an undo request without compensating Azure Table state.</summary>
    /// <param name="context">The saga context whose operation is being undone.</param>
    /// <param name="cancellationToken">The token checked before completing the no-op.</param>
    /// <returns>An already-completed task unless cancellation was requested.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
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

    (Task<global::Azure.Response>, TableEntity) TableInsert(TSaga instance)
    {
        IDictionary<string, object> dict = _context.Converter.GetDictionary(instance);
        var entity = new TableEntity(dict);
        (entity.PartitionKey, entity.RowKey) = _context.Format(instance.CorrelationId);

        return (_context.Table.AddEntityAsync(entity, CancellationToken), entity);
    }

    static ConsumeContext<TMessage> RequireConsumeContext(ConsumeContext<TMessage> consumeContext)
    {
        ArgumentNullException.ThrowIfNull(consumeContext);
        return consumeContext;
    }

    async Task<SagaConsumeContext<TSaga, TMessage>> CreateSagaConsumeContextAsync(TableEntity entity, SagaConsumeContextMode mode)
    {
        var instance = _context.Converter.GetObject(entity);

        SagaConsumeContext<TSaga, TMessage> sagaConsumeContext = await _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, mode)
            .ConfigureAwait(false);

        var eTag = new SagaETag(entity.ETag.ToString());

        sagaConsumeContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        return sagaConsumeContext;
    }
}


sealed class AzureTableLoadSagaRepositoryContext<TSaga> :
    BasePipeContext,
    LoadSagaRepositoryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly DatabaseContext<TSaga> _context;

    public AzureTableLoadSagaRepositoryContext(DatabaseContext<TSaga> context, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        var (partitionKey, rowKey) = _context.Format(correlationId);

        NullableResponse<TableEntity> result = await _context.Table
            .GetEntityIfExistsAsync<TableEntity>(partitionKey, rowKey, cancellationToken: CancellationToken).ConfigureAwait(false);

        return result.HasValue
            ? _context.Converter.GetObject(new TableEntity(result.Value))
            : default;
    }
}
