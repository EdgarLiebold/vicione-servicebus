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

/// <summary>
/// Provides an azure table saga repository context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class AzureTableSagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaRepositoryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext;
    readonly DatabaseContext<TSaga> _context;
    readonly ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> _factory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="factory">The factory value.</param>
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

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

    /// <summary>
    /// Performs the insert operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the save operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); (Task<global::Azure.Response> insert, _) = TableInsert(context.Saga);
        return insert;
    }

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the delete operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
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
