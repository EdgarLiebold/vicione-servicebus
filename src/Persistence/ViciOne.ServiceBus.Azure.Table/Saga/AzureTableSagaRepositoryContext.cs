using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Applies saga repository operations to Azure Table entities within a message consume context.</summary>
/// <typeparam name="TSaga">The saga state persisted by the repository.</typeparam>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
internal sealed class AzureTableSagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    ISagaRepositoryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext;
    readonly IAzureTableSagaStorageContext<TSaga> _context;
    readonly ISagaConsumeContextFactory<IAzureTableSagaStorageContext<TSaga>, TSaga> _factory;

    /// <summary>Creates a repository context for one consumed message.</summary>
    /// <param name="context">The Azure Table access components for the saga type.</param>
    /// <param name="consumeContext">The message consume context that owns cancellation and logging.</param>
    /// <param name="factory">The factory that wraps saga instances in consume contexts.</param>
    public AzureTableSagaRepositoryContext(
        IAzureTableSagaStorageContext<TSaga> context,
        ConsumeContext<TMessage> consumeContext,
        ISagaConsumeContextFactory<IAzureTableSagaStorageContext<TSaga>, TSaga> factory)
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
        ArgumentNullException.ThrowIfNull(instance);

        cancellationToken.ThrowIfCancellationRequested();

        return _factory.CreateSagaConsumeContextAsync(
            _context,
            _consumeContext,
            instance,
            SagaConsumeContextMode.Add);
    }

    /// <summary>Inserts a saga entity and returns its insert-mode consume context.</summary>
    /// <param name="instance">The saga instance to insert.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task whose result is the insert-mode context, or <see langword="null"/> when the entity already exists.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            (Task<global::Azure.Response> insert, var entity) = TableInsert(instance);
            global::Azure.Response response = await insert.ConfigureAwait(false);
            if (response.Headers.ETag is not { } eTag || string.IsNullOrWhiteSpace(eTag.ToString()))
            {
                throw new InvalidOperationException(
                    "Azure Table inserted the saga but returned no usable ETag; conditional saga writes cannot continue.");
            }
            entity.ETag = eTag;
            try
            {
                _consumeContext.LogInsert<TSaga, TMessage>(instance.CorrelationId);
            }
            catch (Exception)
            {
            }

            return await CreateSagaConsumeContextAsync(entity, SagaConsumeContextMode.Insert).ConfigureAwait(false);
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            try
            {
                _consumeContext.LogInsertFault<TSaga, TMessage>(exception, instance.CorrelationId);
            }
            catch (Exception)
            {
            }
            return default;
        }
        catch (Exception exception)
        {
            try
            {
                _consumeContext.LogInsertFault<TSaga, TMessage>(exception, instance.CorrelationId);
            }
            catch (Exception)
            {
            }
            throw;
        }
    }

    /// <summary>Loads a saga by its formatted correlation key and attaches its entity tag for later optimistic writes.</summary>
    /// <param name="correlationId">The non-empty saga correlation identifier.</param>
    /// <param name="cancellationToken">The token checked before loading; Azure I/O uses the surrounding consume-context token.</param>
    /// <returns>A task whose result is the load-mode saga context, or <see langword="null"/> when no entity exists.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
    public async Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        TSaga instance = context.Saga;

        try
        {
            (Task<global::Azure.Response> insert, _) = TableInsert(instance);
            await insert.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            throw new AzureTableSagaConcurrencyException(
                "A saga with the same persistence key already exists.",
                typeof(TSaga),
                instance.CorrelationId,
                exception);
        }
        catch (Exception exception)
        {
            throw new SagaException("Saga save failed", typeof(TSaga), instance.CorrelationId, exception);
        }
    }

    /// <summary>Replaces a persisted saga entity when it still exists and its loaded entity tag matches.</summary>
    /// <param name="context">The saga consume context containing the instance and loaded entity tag.</param>
    /// <param name="cancellationToken">The token checked before the update; Azure I/O uses the cancellation token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Azure Table commits the optimistic replacement.</returns>
    public async Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        TSaga instance = context.Saga;

        try
        {
            var eTag = context.GetPayload<AzureTableSagaETag>();
            IDictionary<string, object> dict = _context.Converter.GetDictionary(instance);
            var entity = new TableEntity(dict) { ETag = new ETag(eTag.ETag) };
            (entity.PartitionKey, entity.RowKey) = _context.Format(instance.CorrelationId);

            await _context.Table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, context.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RequestFailedException exception) when (IsConcurrencyFailure(exception))
        {
            throw new AzureTableSagaConcurrencyException(
                "The persisted saga changed before the update completed.",
                typeof(TSaga),
                instance.CorrelationId,
                exception);
        }
        catch (Exception exception)
        {
            throw new SagaException("Saga update failed", typeof(TSaga), instance.CorrelationId, exception);
        }
    }

    /// <summary>Deletes a persisted saga entity when it still exists and its loaded entity tag matches.</summary>
    /// <param name="context">The saga consume context containing the instance and loaded entity tag.</param>
    /// <param name="cancellationToken">The token checked before deletion; Azure I/O uses the cancellation token carried by <paramref name="context"/>.</param>
    /// <returns>A task that completes when Azure Table commits the optimistic deletion.</returns>
    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        TSaga instance = context.Saga;
        try
        {
            var (partitionKey, rowKey) = _context.Format(instance.CorrelationId);
            var eTag = context.GetPayload<AzureTableSagaETag>();
            await _context.Table
                .DeleteEntityAsync(partitionKey, rowKey, new ETag(eTag.ETag), context.CancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RequestFailedException exception) when (IsConcurrencyFailure(exception))
        {
            throw new AzureTableSagaConcurrencyException(
                "The persisted saga changed before the delete completed.",
                typeof(TSaga),
                instance.CorrelationId,
                exception);
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
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

    /// <summary>Completes an undo request without compensating Azure Table state.</summary>
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

    static bool IsConcurrencyFailure(RequestFailedException exception) =>
        exception.Status == 412
        || (exception.Status == 404
            && string.Equals(exception.ErrorCode, "ResourceNotFound", StringComparison.Ordinal));

    async Task<SagaConsumeContext<TSaga, TMessage>> CreateSagaConsumeContextAsync(TableEntity entity, SagaConsumeContextMode mode)
    {
        var instance = _context.Converter.GetObject(entity);

        SagaConsumeContext<TSaga, TMessage> sagaConsumeContext = await _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, mode)
            .ConfigureAwait(false);

        var eTag = new AzureTableSagaETag(entity.ETag.ToString());

        sagaConsumeContext.AddOrUpdatePayload(() => eTag, _ => eTag);

        return sagaConsumeContext;
    }
}
