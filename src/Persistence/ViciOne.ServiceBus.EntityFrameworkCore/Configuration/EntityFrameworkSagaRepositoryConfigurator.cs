using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures DbContext creation, query shape, transactions, and concurrency for an EF Core saga repository.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class EntityFrameworkSagaRepositoryConfigurator<TSaga> :
    IEntityFrameworkSagaRepositoryConfigurator<TSaga>,
    ISpecification
    where TSaga : class, ISaga
{
    ConcurrencyMode _concurrencyMode;
    Action<ISagaRepositoryRegistrationConfigurator<TSaga>>? _configureDbContext;
    IsolationLevel _isolationLevel;
    ILockStatementProvider? _lockStatementProvider;
    Func<IQueryable<TSaga>, IQueryable<TSaga>>? _queryCustomization;
    bool _isTransactionEnabled = true;

    /// <summary>Initializes a pessimistic repository configuration using serializable transactions.</summary>
    public EntityFrameworkSagaRepositoryConfigurator()
    {
        _isolationLevel = IsolationLevel.Serializable;
        _concurrencyMode = ConcurrencyMode.Pessimistic;
    }

    /// <summary>Sets the isolation level used when the repository creates a transaction.</summary>
    public IsolationLevel IsolationLevel
    {
        set => _isolationLevel = value;
    }

    /// <summary>Registers a transformation applied to all EF Core queries for this saga type.</summary>
    /// <param name="queryCustomization">A function that returns the query the repository should execute.</param>
    public void CustomizeQuery(Func<IQueryable<TSaga>, IQueryable<TSaga>> queryCustomization)
    {
        _queryCustomization = queryCustomization ?? throw new ArgumentNullException(nameof(queryCustomization));
    }

    /// <summary>Sets whether saga rows use optimistic checks or provider-specific pessimistic locks.</summary>
    public ConcurrencyMode ConcurrencyMode
    {
        set => SetConcurrencyMode(value);
    }

    /// <summary>Sets the provider-specific SQL used for pessimistic row locks.</summary>
    public ILockStatementProvider LockStatementProvider
    {
        set => _lockStatementProvider = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Registers a scoped DbContext implementation and uses it for the saga repository.</summary>
    /// <typeparam name="TContext">The service type through which the DbContext is resolved.</typeparam>
    /// <typeparam name="TImplementation">The concrete DbContext type.</typeparam>
    /// <param name="optionsAction">An optional callback that configures the concrete DbContext.</param>
    public void AddDbContext<TContext, TImplementation>(Action<IServiceProvider, DbContextOptionsBuilder<TImplementation>>? optionsAction)
        where TContext : DbContext
        where TImplementation : DbContext, TContext
    {
        _configureDbContext = configurator =>
        {
            AddDbContext<TContext, TImplementation>(configurator, optionsAction);
        };
    }

    /// <summary>Uses a delegate to create a DbContext for each repository scope.</summary>
    /// <param name="databaseFactory">The delegate that creates the DbContext.</param>
    public void DatabaseFactory(Func<DbContext> databaseFactory)
    {
        ArgumentNullException.ThrowIfNull(databaseFactory);
        DatabaseFactory(_ => databaseFactory);
    }

    /// <summary>Uses dependency injection to obtain a delegate that creates a DbContext for each repository scope.</summary>
    /// <param name="databaseFactory">A function that resolves the DbContext factory from the active service provider.</param>
    public void DatabaseFactory(Func<IServiceProvider, Func<DbContext>> databaseFactory)
    {
        ArgumentNullException.ThrowIfNull(databaseFactory);

        _configureDbContext = configurator =>
        {
            configurator.TryAddScoped<ISagaDbContextFactory<TSaga>>(provider => new DelegateSagaDbContextFactory<TSaga>(databaseFactory(provider)));
        };
    }

    /// <summary>Uses an already registered scoped DbContext without taking ownership of its lifetime.</summary>
    /// <typeparam name="TContext">The registered DbContext type.</typeparam>
    public void ExistingDbContext<TContext>()
        where TContext : DbContext
    {
        _configureDbContext = configurator =>
        {
            configurator.TryAddScoped<ISagaDbContextFactory<TSaga>, ContainerSagaDbContextFactory<TContext, TSaga>>();
        };
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_configureDbContext == null)
            yield return this.Failure("DbContext", "must be specified");

        if (_concurrencyMode == ConcurrencyMode.Pessimistic && _lockStatementProvider == null)
            yield return this.Failure("LockStatementProvider", "must be selected explicitly for pessimistic concurrency");
    }

    /// <summary>Registers the configured load, query, and consume repository components.</summary>
    /// <param name="configurator">The saga repository registration to populate.</param>
    public void Register(ISagaRepositoryRegistrationConfigurator<TSaga> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        _configureDbContext?.Invoke(configurator);

        ISagaRepositoryLockStrategy<TSaga> lockStrategy = _concurrencyMode == ConcurrencyMode.Optimistic
            ? CreateOptimisticLockStrategy()
            : CreatePessimisticLockStrategy();

        configurator.TryAddSingleton(_ => lockStrategy);

        configurator.RegisterLoadSagaRepository<TSaga, EntityFrameworkSagaRepositoryContextFactory<TSaga>>();
        configurator.RegisterQuerySagaRepository<TSaga, EntityFrameworkSagaRepositoryContextFactory<TSaga>>();
        configurator
            .RegisterSagaRepository<TSaga, DbContext, SagaConsumeContextFactory<DbContext, TSaga>, EntityFrameworkSagaRepositoryContextFactory<TSaga>>();
    }

    static void AddDbContext<TContext, TImplementation>(IServiceCollection collection,
        Action<IServiceProvider, DbContextOptionsBuilder<TImplementation>>? optionsAction)
        where TImplementation : DbContext, TContext
        where TContext : DbContext
    {
        if (optionsAction != null)
            CheckContextConstructors<TImplementation>();

        collection.TryAddSingleton(provider => DbContextOptionsFactory(provider, optionsAction));
        collection.TryAddScoped<DbContextOptions>(provider => provider.GetRequiredService<DbContextOptions<TImplementation>>());

        collection.TryAddScoped<TContext, TImplementation>();

        collection.TryAddScoped<ISagaDbContextFactory<TSaga>, ContainerSagaDbContextFactory<TContext, TSaga>>();
    }

    static DbContextOptions<TContext> DbContextOptionsFactory<TContext>(IServiceProvider provider,
        Action<IServiceProvider, DbContextOptionsBuilder<TContext>>? optionsAction)
        where TContext : DbContext
    {
        var builder = new DbContextOptionsBuilder<TContext>(new DbContextOptions<TContext>(new Dictionary<Type, IDbContextOptionsExtension>()));

        builder.UseApplicationServiceProvider(provider);

        optionsAction?.Invoke(provider, builder);

        return builder.Options;
    }

    static void CheckContextConstructors<TContext>()
        where TContext : DbContext
    {
        List<ConstructorInfo> declaredConstructors = typeof(TContext).GetTypeInfo().DeclaredConstructors.ToList();
        if (declaredConstructors.Count == 1 && declaredConstructors[0].GetParameters().Length == 0)
            throw new ArgumentException(CoreStrings.DbContextMissingConstructor(typeof(TContext).ShortDisplayName()));
    }

    ISagaRepositoryLockStrategy<TSaga> CreateOptimisticLockStrategy()
    {
        var queryExecutor = new OptimisticLoadQueryExecutor<TSaga>(_queryCustomization);

        return new OptimisticSagaRepositoryLockStrategy<TSaga>(queryExecutor, _queryCustomization, _isolationLevel, _isTransactionEnabled);
    }

    ISagaRepositoryLockStrategy<TSaga> CreatePessimisticLockStrategy()
    {
        var statementProvider = _lockStatementProvider
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "A lock statement provider must be selected explicitly for pessimistic concurrency.", "Correct the named configuration before starting the host"));

        var queryExecutor = new PessimisticLoadQueryExecutor<TSaga>(statementProvider, _queryCustomization);

        return new PessimisticSagaRepositoryLockStrategy<TSaga>(queryExecutor, _queryCustomization, _isolationLevel);
    }

    /// <summary>Selects optimistic concurrency and controls whether repository operations create a transaction.</summary>
    /// <param name="useTransaction"><see langword="true"/> to wrap operations in a transaction; otherwise, <see langword="false"/>.</param>
    public void SetOptimisticConcurrency(bool useTransaction = true)
    {
        SetConcurrencyMode(ConcurrencyMode.Optimistic);
        _isTransactionEnabled = useTransaction;
    }

    void SetConcurrencyMode(ConcurrencyMode concurrencyMode)
    {
        _concurrencyMode = concurrencyMode;
        if (_concurrencyMode == ConcurrencyMode.Optimistic && _isolationLevel == IsolationLevel.Serializable)
            _isolationLevel = IsolationLevel.ReadCommitted;
    }
}
