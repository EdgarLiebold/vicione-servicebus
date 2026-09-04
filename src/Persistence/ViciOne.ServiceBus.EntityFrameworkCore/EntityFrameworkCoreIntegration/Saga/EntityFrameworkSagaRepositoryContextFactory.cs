using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>
/// Provides an entity framework saga repository context factory implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class EntityFrameworkSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>,
    IQuerySagaRepositoryContextFactory<TSaga>,
    ILoadSagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaConsumeContextFactory<DbContext, TSaga> _consumeContextFactory;
    readonly ISagaDbContextFactory<TSaga> _dbContextFactory;
    readonly ISagaRepositoryLockStrategy<TSaga> _lockStrategy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="dbContextFactory">The db context factory value.</param>
    /// <param name="consumeContextFactory">The consume context factory value.</param>
    /// <param name="lockStrategy">The lock strategy value.</param>
    public EntityFrameworkSagaRepositoryContextFactory(ISagaDbContextFactory<TSaga> dbContextFactory,
        ISagaConsumeContextFactory<DbContext, TSaga> consumeContextFactory, ISagaRepositoryLockStrategy<TSaga> lockStrategy)
    {
        _dbContextFactory = dbContextFactory;
        _consumeContextFactory = consumeContextFactory;
        _lockStrategy = lockStrategy;
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="asyncMethod">The async method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<T?> ExecuteAsync<T>(Func<LoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        return ExecuteNullableAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="asyncMethod">The async method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<T> ExecuteAsync<T>(Func<QuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        return ExecuteAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var dbContext = _dbContextFactory.Create();
        try
        {
            context.Add("persistence", "entity-framework");
            context.Add("entities", dbContext.Model.GetEntityTypes().Select(type => type.Name).ToArray());
        }
        finally
        {
            dbContext.Dispose();
        }
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<SagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        var dbContext = _dbContextFactory.CreateScoped(context);
        try
        {
            async Task SendCallbackAsync()
            {
                using var repositoryContext = new DbContextSagaRepositoryContext<TSaga, T>(dbContext, context, _consumeContextFactory, _lockStrategy);

                await next.SendAsync(repositoryContext).ConfigureAwait(false);
            }

            if (context.TryGetPayload(out DbTransactionContext? _))
                await SendCallbackAsync().ConfigureAwait(false);
            else
            {
                var executionStrategy = dbContext.Database.CreateExecutionStrategy();
                await EntityFrameworkExecutionStrategy.ExecuteAsync(
                        dbContext,
                        executionStrategy,
                        context,
                        () => WithinTransactionAsync(dbContext, context.CancellationToken, SendCallbackAsync))
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            await _dbContextFactory.ReleaseAsync(dbContext).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends query.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="query">The query value.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<SagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        var dbContext = _dbContextFactory.CreateScoped(context);
        try
        {
            async Task SendQueryCallbackAsync(SagaLockContext<TSaga> lockContext, SagaRepositoryContext<TSaga, T> repositoryContext)
            {
                IList<TSaga> instances = await lockContext.LoadAsync().ConfigureAwait(false);

                var queryContext = new LoadedSagaRepositoryQueryContext<TSaga, T>(repositoryContext, instances);

                await next.SendAsync(queryContext).ConfigureAwait(false);
            }

            var hasOuterTransaction = context.TryGetPayload(out DbTransactionContext? _);

            async Task SendQueryAsync()
            {
                SagaLockContext<TSaga> lockContext =
                    await _lockStrategy.CreateLockContextAsync(dbContext, query, context.CancellationToken).ConfigureAwait(false);

                using var repositoryContext = new DbContextSagaRepositoryContext<TSaga, T>(dbContext, context, _consumeContextFactory, _lockStrategy);

                if (hasOuterTransaction)
                    await SendQueryCallbackAsync(lockContext, repositoryContext).ConfigureAwait(false);
                else
                {
                    // ReSharper disable once AccessToDisposedClosure
                    await WithinTransactionAsync(dbContext, context.CancellationToken, () => SendQueryCallbackAsync(lockContext, repositoryContext))
                        .ConfigureAwait(false);
                }
            }

            if (hasOuterTransaction)
                await SendQueryAsync().ConfigureAwait(false);
            else
            {
                var executionStrategy = dbContext.Database.CreateExecutionStrategy();
                await EntityFrameworkExecutionStrategy.ExecuteAsync(dbContext, executionStrategy, context, SendQueryAsync).ConfigureAwait(false);
            }
        }
        finally
        {
            await _dbContextFactory.ReleaseAsync(dbContext).ConfigureAwait(false);
        }
    }

    async Task<T> ExecuteAsyncMethodAsync<T>(Func<DbContextSagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken)
        where T : class
    {
        var dbContext = _dbContextFactory.Create();
        try
        {
            Task<T> ExecuteAsync()
            {
                return WithinTransactionAsync(dbContext, cancellationToken, () =>
                {
                    var sagaRepositoryContext = new DbContextSagaRepositoryContext<TSaga>(dbContext, _lockStrategy, cancellationToken);

                    return asyncMethod(sagaRepositoryContext);
                });
            }

            var executionStrategy = dbContext.Database.CreateExecutionStrategy();
            return await EntityFrameworkExecutionStrategy.ExecuteAsync(
                    dbContext,
                    executionStrategy,
                    ExecuteAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await _dbContextFactory.ReleaseAsync(dbContext, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    async Task<T?> ExecuteNullableAsyncMethodAsync<T>(Func<DbContextSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod,
        CancellationToken cancellationToken)
        where T : class
    {
        var dbContext = _dbContextFactory.Create();
        try
        {
            Task<T?> ExecuteAsync()
            {
                return WithinTransactionAsync(dbContext, cancellationToken, () =>
                {
                    var sagaRepositoryContext = new DbContextSagaRepositoryContext<TSaga>(dbContext, _lockStrategy, cancellationToken);

                    return asyncMethod(sagaRepositoryContext);
                });
            }

            var executionStrategy = dbContext.Database.CreateExecutionStrategy();
            return await EntityFrameworkExecutionStrategy.ExecuteAsync(
                    dbContext,
                    executionStrategy,
                    ExecuteAsync,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await _dbContextFactory.ReleaseAsync(dbContext, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    Task WithinTransactionAsync(DbContext context, CancellationToken cancellationToken, Func<Task> callback)
    {
        if (!_lockStrategy.IsTransactionEnabled)
            return callback();

        async Task<bool> CreateAsync()
        {
            await callback().ConfigureAwait(false);
            return true;
        }

        return WithinTransactionAsync(context, cancellationToken, CreateAsync);
    }

    async Task<T> WithinTransactionAsync<T>(DbContext context, CancellationToken cancellationToken, Func<Task<T>> callback)
    {
        if (!_lockStrategy.IsTransactionEnabled)
            return await callback().ConfigureAwait(false);

        await using var transaction = await context.Database.BeginTransactionAsync(_lockStrategy.IsolationLevel, cancellationToken).ConfigureAwait(false);

        static async Task RollbackAsync(IDbContextTransaction transaction)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                //
            }
        }

        try
        {
            var result = await callback().ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            throw;
        }
        catch (DbUpdateException)
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            throw;
        }
        catch (Exception)
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            throw;
        }
    }
}
