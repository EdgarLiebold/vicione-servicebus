using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.DynamoDbIntegration.Saga;

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

    public DynamoDbSagaRepositoryContext(DatabaseContext<TSaga> context, ConsumeContext<TMessage> consumeContext,
        ISagaConsumeContextFactory<DatabaseContext<TSaga>, TSaga> factory)
        : base(consumeContext)
    {
        _context = context;
        _consumeContext = consumeContext;
        _factory = factory;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

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

    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instance = await _context.LoadAsync(correlationId, _consumeContext.CancellationToken).ConfigureAwait(false);
        if (instance == null)
            return default;

        return await _factory.CreateSagaConsumeContextAsync(_context, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
    }

    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _context.AddAsync(context.Saga, context.CancellationToken);
    }

    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _context.UpdateAsync(context.Saga, context.CancellationToken);
    }

    public Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return _context.DeleteAsync(context.Saga, context.CancellationToken);
    }

    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return TaskResults.Completed;
    }

    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return TaskResults.Completed;
    }

    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _factory.CreateSagaConsumeContextAsync(_context, consumeContext, instance, mode);
    }
}


public class DynamoDbSagaRepositoryContext<TSaga> :
    BasePipeContext,
    LoadSagaRepositoryContext<TSaga>,
    IDisposable
    where TSaga : class, ISagaVersion
{
    readonly DatabaseContext<TSaga> _context;

    public DynamoDbSagaRepositoryContext(DatabaseContext<TSaga> context, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _context = context;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TSaga?>(cancellationToken); return _context.LoadAsync(correlationId, CancellationToken);
    }
}
