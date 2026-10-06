using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Saga;

/// <summary>Executes EF Core saga load, query, and consume operations with the configured transaction strategy.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class EntityFrameworkSagaRepositoryContextFactory<TSaga> :
    ISagaRepositoryContextFactory<TSaga>,
    IQuerySagaRepositoryContextFactory<TSaga>,
    ILoadSagaRepositoryContextFactory<TSaga>
    where TSaga : class, ISaga
{
    readonly ISagaConsumeContextFactory<DbContext, TSaga> _consumeContextFactory;
    readonly ISagaDbContextFactory<TSaga> _dbContextFactory;
    readonly ISagaRepositoryLockStrategy<TSaga> _lockStrategy;

    /// <summary>Initializes the repository-context factory with DbContext, consume-context, and lock-strategy factories.</summary>
    /// <param name="dbContextFactory">The factory that supplies and releases DbContext instances.</param>
    /// <param name="consumeContextFactory">The factory that wraps loaded entities for message consumption.</param>
    /// <param name="lockStrategy">The concurrency, transaction, and query strategy.</param>
    public EntityFrameworkSagaRepositoryContextFactory(ISagaDbContextFactory<TSaga> dbContextFactory,
        ISagaConsumeContextFactory<DbContext, TSaga> consumeContextFactory, ISagaRepositoryLockStrategy<TSaga> lockStrategy)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _consumeContextFactory = consumeContextFactory ?? throw new ArgumentNullException(nameof(consumeContextFactory));
        _lockStrategy = lockStrategy ?? throw new ArgumentNullException(nameof(lockStrategy));
    }

    /// <summary>Executes a nullable load operation with provider retries and the configured transaction.</summary>
    /// <typeparam name="T">The reference-type result.</typeparam>
    /// <param name="asyncMethod">The operation to execute against a load context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The value returned by <paramref name="asyncMethod" />, which may be <see langword="null"/>.</returns>
    public Task<T?> ExecuteAsync<T>(Func<ILoadSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(asyncMethod);
        cancellationToken.ThrowIfCancellationRequested();

        return ExecuteNullableAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    /// <summary>Executes a query operation with provider retries and the configured transaction.</summary>
    /// <typeparam name="T">The reference-type result.</typeparam>
    /// <param name="asyncMethod">The operation to execute against a query context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The value returned by <paramref name="asyncMethod" />.</returns>
    public Task<T> ExecuteAsync<T>(Func<IQuerySagaRepositoryContext<TSaga>, Task<T>> asyncMethod, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(asyncMethod);
        cancellationToken.ThrowIfCancellationRequested();

        return ExecuteAsyncMethodAsync(asyncMethod, cancellationToken);
    }

    /// <summary>Adds the EF Core persistence identity and mapped entity names to the probe.</summary>
    /// <param name="context">The probe context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var dbContext = _dbContextFactory.CreateDbContext();
        try
        {
            context.Add("persistence", "entity-framework");
            context.Add("entities", dbContext.Model.GetEntityTypes().Select(type => type.Name).ToArray());
        }
        finally
        {
            _dbContextFactory.ReleaseAsync(dbContext).GetAwaiter().GetResult();
        }
    }

    /// <summary>Executes a saga consume pipeline in the ambient or repository-created transaction.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The active consumption context.</param>
    /// <param name="next">The saga repository pipeline to execute.</param>
    /// <returns>A task that completes after the consume pipeline and its transaction have finished.</returns>
    public async Task SendAsync<T>(ConsumeContext<T> context, IPipe<ISagaRepositoryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        context.CancellationToken.ThrowIfCancellationRequested();

        var dbContext = _dbContextFactory.CreateScopedDbContext(context);
        try
        {
            async Task SendCallbackAsync()
            {
                using var repositoryContext = new DbContextSagaRepositoryContext<TSaga, T>(dbContext, context, _consumeContextFactory, _lockStrategy);

                await next.SendAsync(repositoryContext).ConfigureAwait(false);
            }

            if (context.TryGetPayload(out IDbTransactionContext? transactionContext)
                && dbContext.Database.CurrentTransaction is { } currentTransaction
                && currentTransaction.TransactionId == transactionContext.TransactionId)
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

    /// <summary>Loads all saga rows selected by a query and executes the query pipeline.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The active consumption context.</param>
    /// <param name="query">The saga filter to execute.</param>
    /// <param name="next">The loaded-saga query pipeline to execute.</param>
    /// <returns>A task that completes after every selected saga has passed through the query pipeline.</returns>
    public async Task SendQueryAsync<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, IPipe<ISagaRepositoryQueryContext<TSaga, T>> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(next);
        context.CancellationToken.ThrowIfCancellationRequested();

        var dbContext = _dbContextFactory.CreateScopedDbContext(context);
        try
        {
            async Task SendQueryCallbackAsync(SagaLockContext<TSaga> lockContext, ISagaRepositoryContext<TSaga, T> repositoryContext)
            {
                IList<TSaga> instances = await lockContext.LoadAsync().ConfigureAwait(false);

                var queryContext = new LoadedSagaRepositoryQueryContext<TSaga, T>(repositoryContext, instances);

                await next.SendAsync(queryContext).ConfigureAwait(false);
            }

            var hasOuterTransaction = context.TryGetPayload(out IDbTransactionContext? transactionContext)
                && dbContext.Database.CurrentTransaction is { } currentTransaction
                && currentTransaction.TransactionId == transactionContext.TransactionId;

            async Task SendQueryAsync()
            {
                SagaLockContext<TSaga> lockContext =
                    await _lockStrategy.CreateLockContextAsync(dbContext, query, context.CancellationToken).ConfigureAwait(false);

                using var repositoryContext = new DbContextSagaRepositoryContext<TSaga, T>(dbContext, context, _consumeContextFactory, _lockStrategy);

                if (hasOuterTransaction)
                    await SendQueryCallbackAsync(lockContext, repositoryContext).ConfigureAwait(false);
                else
                {
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
        var dbContext = _dbContextFactory.CreateDbContext();
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
            await _dbContextFactory.ReleaseAsync(dbContext).ConfigureAwait(false);
        }
    }

    async Task<T?> ExecuteNullableAsyncMethodAsync<T>(Func<DbContextSagaRepositoryContext<TSaga>, Task<T?>> asyncMethod,
        CancellationToken cancellationToken)
        where T : class
    {
        var dbContext = _dbContextFactory.CreateDbContext();
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
            await _dbContextFactory.ReleaseAsync(dbContext).ConfigureAwait(false);
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
                // Preserve the operation failure when the provider also fails during best-effort rollback.
            }
        }

        try
        {
            var result = await callback().ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch (Exception)
        {
            await RollbackAsync(transaction).ConfigureAwait(false);
            throw;
        }
    }
}
