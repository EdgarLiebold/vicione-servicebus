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

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Executes saga persistence for one consumed message through a shared EF Core DbContext.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
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

    /// <summary>Initializes a message-scoped saga repository context over a tracked DbContext.</summary>
    /// <param name="dbContext">The DbContext that tracks saga state.</param>
    /// <param name="consumeContext">The active message-consumption context.</param>
    /// <param name="factory">The factory that wraps saga instances for consumption.</param>
    /// <param name="lockStrategy">The configured query and concurrency strategy.</param>
    public DbContextSagaRepositoryContext(DbContext dbContext, ConsumeContext<TMessage> consumeContext,
        ISagaConsumeContextFactory<DbContext, TSaga> factory, ISagaRepositoryLockStrategy<TSaga> lockStrategy)
        : base(consumeContext, dbContext)
    {
        _dbContext = dbContext;
        _consumeContext = consumeContext;
        _factory = factory;
        _lockStrategy = lockStrategy;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _inUse.Dispose();
    }

    /// <summary>Creates an add-mode consume context for a new saga instance without persisting it yet.</summary>
    /// <param name="instance">The new saga instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A consume context that will add the saga when the pipeline saves it.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_dbContext, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

    /// <summary>Inserts a saga immediately, returning <see langword="null"/> when a concurrent insert won the same identity.</summary>
    /// <param name="instance">The saga instance to insert.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An insert-mode consume context, or <see langword="null"/> after a verified identity race.</returns>
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

    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A load-mode consume context, or <see langword="null"/> when the saga does not exist.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instance = await _lockStrategy.LoadAsync(_dbContext, correlationId, CancellationToken).ConfigureAwait(false);
        if (instance == null)
            return default;

        return await _factory.CreateSagaConsumeContextAsync(_dbContext, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
    }

    /// <summary>Adds a newly created saga to the DbContext and saves changes.</summary>
    /// <param name="context">The saga consume context containing the new instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Saves tracked changes for an existing saga.</summary>
    /// <param name="context">The saga consume context containing the tracked instance.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Deletes the saga instance and saves changes.</summary>
    /// <param name="context">The saga consume context containing the instance to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Completes without changing tracked state.</summary>
    /// <param name="context">The saga consume context whose tracked changes are left untouched.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Marks the tracked saga entity unchanged so its pending modifications are not saved.</summary>
    /// <param name="context">The saga consume context whose tracked entity is reverted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Wraps a saga instance for another message contract using the same DbContext.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="consumeContext">The active consumption context.</param>
    /// <param name="instance">The saga instance to wrap.</param>
    /// <param name="mode">The repository operation represented by the context.</param>
    /// <returns>The created saga consume context.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance, SagaConsumeContextMode mode)
        where T : class
    {
        return _factory.CreateSagaConsumeContextAsync(_dbContext, consumeContext, instance, mode);
    }
}


/// <summary>Executes direct saga loads and queries through one EF Core DbContext.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class DbContextSagaRepositoryContext<TSaga> :
    BasePipeContext,
    QuerySagaRepositoryContext<TSaga>,
    LoadSagaRepositoryContext<TSaga>
    where TSaga : class, ISaga
{
    readonly DbContext _dbContext;
    readonly ISagaRepositoryLockStrategy<TSaga> _lockStrategy;

    /// <summary>Initializes a query-scoped saga repository context over a tracked DbContext.</summary>
    /// <param name="dbContext">The DbContext that contains the saga set.</param>
    /// <param name="lockStrategy">The configured query and concurrency strategy.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public DbContextSagaRepositoryContext(DbContext dbContext, ISagaRepositoryLockStrategy<TSaga> lockStrategy,
        CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _lockStrategy = lockStrategy ?? throw new ArgumentNullException(nameof(lockStrategy));
    }

    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The tracked saga entity, or <see langword="null"/> when no row matches.</returns>
    public Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TSaga?>(cancellationToken); return _lockStrategy.LoadAsync(_dbContext, correlationId, CancellationToken);
    }

    /// <summary>Loads the correlation identifiers selected by a saga query.</summary>
    /// <param name="query">The saga filter to execute.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A query context containing the matching correlation identifiers.</returns>
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
