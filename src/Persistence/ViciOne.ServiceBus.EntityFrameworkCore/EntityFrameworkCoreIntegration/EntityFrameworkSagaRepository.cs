using System;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides an entity framework saga repository implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public static class EntityFrameworkSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Creates optimistic.
    /// </summary>
    /// <param name="dbContextFactory">The db context factory value.</param>
    /// <param name="queryCustomization">The query customization value.</param>
    /// <param name="isTransactionEnabled">The is transaction enabled value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaRepository<TSaga> CreateOptimistic(ISagaDbContextFactory<TSaga> dbContextFactory,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null, bool isTransactionEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        var queryExecutor = new OptimisticLoadQueryExecutor<TSaga>(queryCustomization);
        var lockStrategy = new OptimisticSagaRepositoryLockStrategy<TSaga>(queryExecutor, queryCustomization, IsolationLevel.ReadCommitted, isTransactionEnabled);

        return CreateRepository(dbContextFactory, lockStrategy);
    }

    /// <summary>
    /// Creates optimistic.
    /// </summary>
    /// <param name="dbContextFactory">The db context factory value.</param>
    /// <param name="queryCustomization">The query customization value.</param>
    /// <param name="isTransactionEnabled">The is transaction enabled value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaRepository<TSaga> CreateOptimistic(Func<DbContext> dbContextFactory,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null, bool isTransactionEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        return CreateOptimistic(new DelegateSagaDbContextFactory<TSaga>(dbContextFactory), queryCustomization, isTransactionEnabled);
    }

    /// <summary>
    /// Creates pessimistic.
    /// </summary>
    /// <param name="dbContextFactory">The db context factory value.</param>
    /// <param name="lockStatementProvider">The lock statement provider value.</param>
    /// <param name="queryCustomization">The query customization value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaRepository<TSaga> CreatePessimistic(ISagaDbContextFactory<TSaga> dbContextFactory,
        ILockStatementProvider lockStatementProvider,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(lockStatementProvider);

        var queryExecutor = new PessimisticLoadQueryExecutor<TSaga>(lockStatementProvider, queryCustomization);
        var lockStrategy = new PessimisticSagaRepositoryLockStrategy<TSaga>(queryExecutor, queryCustomization, IsolationLevel.Serializable);

        return CreateRepository(dbContextFactory, lockStrategy);
    }

    /// <summary>
    /// Creates pessimistic.
    /// </summary>
    /// <param name="dbContextFactory">The db context factory value.</param>
    /// <param name="lockStatementProvider">The lock statement provider value.</param>
    /// <param name="queryCustomization">The query customization value.</param>
    /// <returns>The result of the operation.</returns>
    public static ISagaRepository<TSaga> CreatePessimistic(Func<DbContext> dbContextFactory, ILockStatementProvider lockStatementProvider,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        ArgumentNullException.ThrowIfNull(lockStatementProvider);

        return CreatePessimistic(new DelegateSagaDbContextFactory<TSaga>(dbContextFactory), lockStatementProvider, queryCustomization);
    }

    static ISagaRepository<TSaga> CreateRepository(ISagaDbContextFactory<TSaga> dbContextFactory, ISagaRepositoryLockStrategy<TSaga> lockStrategy)
    {
        var consumeContextFactory = new SagaConsumeContextFactory<DbContext, TSaga>();

        var repositoryFactory =
            new EntityFrameworkSagaRepositoryContextFactory<TSaga>(dbContextFactory, consumeContextFactory, lockStrategy);

        return new SagaRepository<TSaga>(repositoryFactory, repositoryFactory, repositoryFactory);
    }
}
