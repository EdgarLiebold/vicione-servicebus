using System;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for entity framework saga repository configurator.
/// </summary>
public interface IEntityFrameworkSagaRepositoryConfigurator
{
    /// <summary>
    /// Gets or sets the concurrency mode value.
    /// </summary>
    ConcurrencyMode ConcurrencyMode { set; }
    /// <summary>
    /// Gets or sets the isolation level value.
    /// </summary>
    IsolationLevel IsolationLevel { set; }
    /// <summary>
    /// Gets or sets the lock statement provider value.
    /// </summary>
    ILockStatementProvider LockStatementProvider { set; }

    /// <summary>
    /// Add the DbContext to the container, and configure the repository to use it
    /// </summary>
    /// <param name="optionsAction"></param>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    void AddDbContext<TContext, TImplementation>(Action<IServiceProvider, DbContextOptionsBuilder<TImplementation>>? optionsAction = null)
        where TContext : DbContext
        where TImplementation : DbContext, TContext;

    /// <summary>
    /// Use a simple factory method to create the database
    /// </summary>
    /// <param name="databaseFactory"></param>
    void DatabaseFactory(Func<DbContext> databaseFactory);

    /// <summary>
    /// Use the configuration service provider to resolve the database factory
    /// </summary>
    /// <param name="databaseFactory"></param>
    void DatabaseFactory(Func<IServiceProvider, Func<DbContext>> databaseFactory);

    /// <summary>
    /// Use an existing (already configured in the container) DbContext that will be resolved
    /// within the container scope
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    void ExistingDbContext<TContext>()
        where TContext : DbContext;

    /// <summary>
    /// Configures the saga to use optimistic concurrency, with optional transaction support.
    /// </summary>
    /// <param name="useTransaction">
    /// If <c>true</c>, operations on the saga will be executed within a transaction;
    /// if <c>false</c>, no transaction will be used.
    /// </param>
    void SetOptimisticConcurrency(bool useTransaction = true);
}


/// <summary>
/// Defines the contract for entity framework saga repository configurator.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface IEntityFrameworkSagaRepositoryConfigurator<TSaga> :
    IEntityFrameworkSagaRepositoryConfigurator
    where TSaga : class, ISaga
{
    /// <summary>
    /// Use custom query
    /// </summary>
    /// <param name="queryCustomization"></param>
    void CustomizeQuery(Func<IQueryable<TSaga>, IQueryable<TSaga>> queryCustomization);
}
