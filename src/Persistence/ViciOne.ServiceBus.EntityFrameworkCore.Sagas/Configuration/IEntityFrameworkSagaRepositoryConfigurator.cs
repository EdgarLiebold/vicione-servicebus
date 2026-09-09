using System;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Configures DbContext creation, transactions, and concurrency for an EF Core saga repository.</summary>
public interface IEntityFrameworkSagaRepositoryConfigurator
{
    /// <summary>Sets whether saga rows use optimistic checks or provider-specific pessimistic locks.</summary>
    ConcurrencyMode ConcurrencyMode { set; }
    /// <summary>Sets the isolation level used when the repository creates a transaction.</summary>
    IsolationLevel IsolationLevel { set; }
    /// <summary>Sets the provider-specific SQL used for pessimistic row locks.</summary>
    ILockStatementProvider LockStatementProvider { set; }

    /// <summary>Registers a DbContext implementation and configures the repository to use it.</summary>
    /// <typeparam name="TContext">The service type through which the DbContext is resolved.</typeparam>
    /// <typeparam name="TImplementation">The concrete DbContext type.</typeparam>
    /// <param name="optionsAction">An optional callback that configures the concrete DbContext.</param>
    void AddDbContext<TContext, TImplementation>(Action<IServiceProvider, DbContextOptionsBuilder<TImplementation>>? optionsAction = null)
        where TContext : DbContext
        where TImplementation : DbContext, TContext;

    /// <summary>Uses a delegate to create a DbContext for each repository scope.</summary>
    /// <param name="dbContextFactory">The delegate that creates the DbContext.</param>
    void UseDbContextFactory(Func<DbContext> dbContextFactory);

    /// <summary>Uses the active service provider to resolve a DbContext factory for each repository scope.</summary>
    /// <param name="dbContextFactoryResolver">A function that resolves the DbContext factory from the active service provider.</param>
    void UseDbContextFactory(Func<IServiceProvider, Func<DbContext>> dbContextFactoryResolver);

    /// <summary>
    /// Use an existing (already configured in the container) DbContext that will be resolved
    /// within the container scope.
    /// </summary>
    /// <typeparam name="TContext">The registered DbContext type.</typeparam>
    void UseExistingDbContext<TContext>()
        where TContext : DbContext;

    /// <summary>Configures the saga to use optimistic concurrency, with optional transaction support.</summary>
    /// <param name="useTransaction">
    /// If <c>true</c>, operations on the saga will be executed within a transaction;
    /// if <c>false</c>, no transaction will be used.
    /// </param>
    void SetOptimisticConcurrency(bool useTransaction = true);
}

/// <summary>Adds saga-type-specific EF Core query configuration.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface IEntityFrameworkSagaRepositoryConfigurator<TSaga> :
    IEntityFrameworkSagaRepositoryConfigurator
    where TSaga : class, ISaga
{
    /// <summary>Registers a transformation applied to all EF Core queries for this saga type.</summary>
    /// <param name="queryCustomization">A function that returns the query the repository should execute.</param>
    void CustomizeQuery(Func<IQueryable<TSaga>, IQueryable<TSaga>> queryCustomization);
}
