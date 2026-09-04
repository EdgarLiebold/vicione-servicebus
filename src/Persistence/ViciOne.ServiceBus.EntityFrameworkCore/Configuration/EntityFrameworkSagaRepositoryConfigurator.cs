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

/// <summary>
/// Provides an entity framework saga repository configurator implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public EntityFrameworkSagaRepositoryConfigurator()
    {
        _isolationLevel = IsolationLevel.Serializable;
        _concurrencyMode = ConcurrencyMode.Pessimistic;
    }

    /// <summary>
    /// Gets or sets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel
    {
        set => _isolationLevel = value;
    }

    /// <summary>
    /// Performs the customize query operation.
    /// </summary>
    /// <param name="queryCustomization">The query customization value.</param>
    public void CustomizeQuery(Func<IQueryable<TSaga>, IQueryable<TSaga>> queryCustomization)
    {
        _queryCustomization = queryCustomization ?? throw new ArgumentNullException(nameof(queryCustomization));
    }

    /// <summary>
    /// Gets or sets the concurrency mode value.
    /// </summary>
    public ConcurrencyMode ConcurrencyMode
    {
        set => SetConcurrencyMode(value);
    }

    /// <summary>
    /// Gets or sets the lock statement provider value.
    /// </summary>
    public ILockStatementProvider LockStatementProvider
    {
        set => _lockStatementProvider = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Adds db context to the configuration.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <typeparam name="TImplementation">The t implementation type.</typeparam>
    /// <param name="optionsAction">The options action value.</param>
    public void AddDbContext<TContext, TImplementation>(Action<IServiceProvider, DbContextOptionsBuilder<TImplementation>>? optionsAction)
        where TContext : DbContext
        where TImplementation : DbContext, TContext
    {
        _configureDbContext = configurator =>
        {
            AddDbContext<TContext, TImplementation>(configurator, optionsAction);
        };
    }

    /// <summary>
    /// Performs the database factory operation.
    /// </summary>
    /// <param name="databaseFactory">The database factory value.</param>
    public void DatabaseFactory(Func<DbContext> databaseFactory)
    {
        ArgumentNullException.ThrowIfNull(databaseFactory);
        DatabaseFactory(_ => databaseFactory);
    }

    /// <summary>
    /// Performs the database factory operation.
    /// </summary>
    /// <param name="databaseFactory">The database factory value.</param>
    public void DatabaseFactory(Func<IServiceProvider, Func<DbContext>> databaseFactory)
    {
        ArgumentNullException.ThrowIfNull(databaseFactory);

        _configureDbContext = configurator =>
        {
            configurator.TryAddScoped<ISagaDbContextFactory<TSaga>>(provider => new DelegateSagaDbContextFactory<TSaga>(databaseFactory(provider)));
        };
    }

    /// <summary>
    /// Performs the existing db context operation.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    public void ExistingDbContext<TContext>()
        where TContext : DbContext
    {
        _configureDbContext = configurator =>
        {
            configurator.TryAddScoped<ISagaDbContextFactory<TSaga>, ContainerSagaDbContextFactory<TContext, TSaga>>();
        };
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_configureDbContext == null)
            yield return this.Failure("DbContext", "must be specified");

        if (_concurrencyMode == ConcurrencyMode.Pessimistic && _lockStatementProvider == null)
            yield return this.Failure("LockStatementProvider", "must be selected explicitly for pessimistic concurrency");
    }

    /// <summary>
    /// Performs the register operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
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
            ?? throw new ConfigurationException("A lock statement provider must be selected explicitly for pessimistic concurrency.");

        var queryExecutor = new PessimisticLoadQueryExecutor<TSaga>(statementProvider, _queryCustomization);

        return new PessimisticSagaRepositoryLockStrategy<TSaga>(queryExecutor, _queryCustomization, _isolationLevel);
    }

    /// <summary>
    /// Sets optimistic concurrency.
    /// </summary>
    /// <param name="useTransaction">The use transaction value.</param>
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
