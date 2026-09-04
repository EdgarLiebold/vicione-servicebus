using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Saga;

public class DbContextSagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaRepositoryContext<TSaga, TMessage>,
    IDisposable
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext;
    readonly DbContext _dbContext;
    readonly ISagaConsumeContextFactory<DbContext, TSaga> _factory;
    readonly SemaphoreSlim _inUse = new SemaphoreSlim(1);
    readonly ISagaRepositoryLockStrategy<TSaga> _lockStrategy;

    public DbContextSagaRepositoryContext(DbContext dbContext, ConsumeContext<TMessage> consumeContext,
        ISagaConsumeContextFactory<DbContext, TSaga> factory, ISagaRepositoryLockStrategy<TSaga> lockStrategy)
        : base(consumeContext, dbContext)
    {
        _dbContext = dbContext;
        _consumeContext = consumeContext;
        _factory = factory;
        _lockStrategy = lockStrategy;
    }

    public void Dispose()
    {
        _inUse.Dispose();
    }

    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_dbContext, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

    public async Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); EntityEntry<TSaga> entry = await _dbContext.Set<TSaga>().AddAsync(instance, CancellationToken).ConfigureAwait(false);
        try
        {
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);

            _consumeContext.LogInsert<TSaga, TMessage>(instance.CorrelationId);

            return await _factory.CreateSagaConsumeContextAsync(_dbContext, _consumeContext, instance, SagaConsumeContextMode.Insert).ConfigureAwait(false);
        }
        catch (DbUpdateException exception)
        {
            // Insert-on-initial can lose a race to another consumer. Provider exception codes are
            // not a portable proof of that condition. The failed EF entry must be this exact saga
            // instance and the exact saga identity must now be loadable through the configured lock
            // strategy. Every other persistence failure remains the original failure and is never
            // converted into a missing Insert result.
            if (exception.Entries.Any(failedEntry => ReferenceEquals(failedEntry.Entity, instance)) == false)
                throw;

            entry.State = EntityState.Detached;

            TSaga? existing;
            try
            {
                existing = await _lockStrategy.LoadAsync(_dbContext, instance.CorrelationId, CancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }

            if (existing == null)
                throw;

            _consumeContext.LogInsertFault<TSaga, TMessage>(exception, instance.CorrelationId);

            return default;
        }
    }

    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instance = await _lockStrategy.LoadAsync(_dbContext, correlationId, CancellationToken).ConfigureAwait(false);
        if (instance == null)
            return default;

        return await _factory.CreateSagaConsumeContextAsync(_dbContext, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
    }

    public async Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        await _inUse.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            await _dbContext.Set<TSaga>().AddAsync(context.Saga, cancellationToken: cancellationToken).ConfigureAwait(false);

            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException("Saga save failed", typeof(TSaga), context.Saga.CorrelationId, exception);
        }
        finally
        {
            _inUse.Release();
        }
    }

    public async Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _inUse.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException("Saga update failed", typeof(TSaga), context.Saga.CorrelationId, exception);
        }
        finally
        {
            _inUse.Release();
        }
    }

    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _inUse.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            _dbContext.Set<TSaga>().Remove(context.Saga);

            await _dbContext.SaveChangesAsync(CancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException("Saga delete failed", typeof(TSaga), context.Saga.CorrelationId, exception);
        }
        finally
        {
            _inUse.Release();
        }
    }

    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    public async Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); await _inUse.WaitAsync(context.CancellationToken).ConfigureAwait(false);
        try
        {
            var entity = _dbContext.ChangeTracker.Entries<TSaga>().FirstOrDefault(x => x.Entity.CorrelationId == context.Saga.CorrelationId);
            if (entity != null)
                entity.State = EntityState.Unchanged;
        }
        finally
        {
            _inUse.Release();
        }
    }

    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _factory.CreateSagaConsumeContextAsync(_dbContext, consumeContext, instance, mode);
    }
}


public class DbContextSagaRepositoryContext<TSaga> :
    BasePipeContext,
    QuerySagaRepositoryContext<TSaga>,
    LoadSagaRepositoryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly DbContext _dbContext;
    readonly ISagaRepositoryLockStrategy<TSaga> _lockStrategy;

    public DbContextSagaRepositoryContext(DbContext dbContext, ISagaRepositoryLockStrategy<TSaga> lockStrategy,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _lockStrategy = lockStrategy ?? throw new ArgumentNullException(nameof(lockStrategy));
    }

    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TSaga?>(cancellationToken); return _lockStrategy.LoadAsync(_dbContext, correlationId, CancellationToken);
    }

    public async Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IList<Guid> results = await _lockStrategy.ApplyQueryCustomization(_dbContext.Set<TSaga>())
            .AsNoTracking()
            .Where(query.FilterExpression)
            .Select(x => x.CorrelationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new DefaultSagaRepositoryQueryContext<TSaga>(this, results);
    }
}
