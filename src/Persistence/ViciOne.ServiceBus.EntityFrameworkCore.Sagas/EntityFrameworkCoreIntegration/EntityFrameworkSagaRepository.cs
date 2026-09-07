using System;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Creates standalone EF Core saga repositories with optimistic or pessimistic concurrency.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public static class EntityFrameworkSagaRepository<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates an optimistic repository from an explicit saga DbContext factory.</summary>
    /// <param name="dbContextFactory">The factory that supplies and releases DbContext instances.</param>
    /// <param name="queryCustomization">An optional transformation applied to every saga query.</param>
    /// <param name="isTransactionEnabled"><see langword="true"/> to wrap repository operations in read-committed transactions.</param>
    /// <returns>An EF Core saga repository using optimistic concurrency.</returns>
    public static ISagaRepository<TSaga> CreateOptimistic(ISagaDbContextFactory<TSaga> dbContextFactory,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null, bool isTransactionEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        var queryExecutor = new OptimisticLoadQueryExecutor<TSaga>(queryCustomization);
        var lockStrategy = new OptimisticSagaRepositoryLockStrategy<TSaga>(queryExecutor, queryCustomization, IsolationLevel.ReadCommitted, isTransactionEnabled);

        return CreateRepository(dbContextFactory, lockStrategy);
    }

    /// <summary>Creates an optimistic repository from a DbContext creation delegate.</summary>
    /// <param name="dbContextFactory">The delegate that creates a DbContext for each repository scope.</param>
    /// <param name="queryCustomization">An optional transformation applied to every saga query.</param>
    /// <param name="isTransactionEnabled"><see langword="true"/> to wrap repository operations in read-committed transactions.</param>
    /// <returns>An EF Core saga repository using optimistic concurrency.</returns>
    public static ISagaRepository<TSaga> CreateOptimistic(Func<DbContext> dbContextFactory,
        Func<IQueryable<TSaga>, IQueryable<TSaga>>? queryCustomization = null, bool isTransactionEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);

        return CreateOptimistic(new DelegateSagaDbContextFactory<TSaga>(dbContextFactory), queryCustomization, isTransactionEnabled);
    }

    /// <summary>Creates a serializable pessimistic repository from an explicit saga DbContext factory.</summary>
    /// <param name="dbContextFactory">The factory that supplies and releases DbContext instances.</param>
    /// <param name="lockStatementProvider">The relational provider that generates row-lock SQL.</param>
    /// <param name="queryCustomization">An optional transformation applied to every saga query.</param>
    /// <returns>An EF Core saga repository using provider-specific row locks.</returns>
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

    /// <summary>Creates a serializable pessimistic repository from a DbContext creation delegate.</summary>
    /// <param name="dbContextFactory">The delegate that creates a DbContext for each repository scope.</param>
    /// <param name="lockStatementProvider">The relational provider that generates row-lock SQL.</param>
    /// <param name="queryCustomization">An optional transformation applied to every saga query.</param>
    /// <returns>An EF Core saga repository using provider-specific row locks.</returns>
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
