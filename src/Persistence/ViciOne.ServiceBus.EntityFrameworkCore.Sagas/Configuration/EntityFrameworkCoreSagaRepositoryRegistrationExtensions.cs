using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Configuration;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Provides EF Core saga-repository registration and relational-provider selection.</summary>
public static class EntityFrameworkCoreSagaRepositoryRegistrationExtensions
{
    /// <summary>Registers an EF Core repository for the saga type.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="configurator">The saga registration to associate with the EF Core repository.</param>
    /// <param name="configure">The callback that selects the DbContext and concurrency behavior.</param>
    /// <returns>The same saga registration configurator.</returns>
    public static ISagaRegistrationConfigurator<TSaga> EntityFrameworkRepository<TSaga>(this ISagaRegistrationConfigurator<TSaga> configurator,
        Action<IEntityFrameworkSagaRepositoryConfigurator<TSaga>> configure)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        var repositoryConfigurator = new EntityFrameworkSagaRepositoryConfigurator<TSaga>();

        configure(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The Entity Framework saga repository configuration is invalid:");

        configurator.Repository(x => repositoryConfigurator.Register(x));

        return configurator;
    }

    /// <summary>Registers the saga in a shared EF Core repository and optionally configures its entity mapping.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="configurator">The saga registration to associate with the shared EF Core repository.</param>
    /// <param name="sagaRepository">The shared mapping and DbContext repository.</param>
    /// <param name="configure">An optional callback that configures concurrency and querying.</param>
    /// <param name="configureSagaMapping">An optional callback that configures the saga entity.</param>
    /// <returns>The same saga registration configurator.</returns>
    public static ISagaRegistrationConfigurator<TSaga> EntityFrameworkRepository<TSaga>(this ISagaRegistrationConfigurator<TSaga> configurator,
        IEntityFrameworkSagaRepository sagaRepository, Action<IEntityFrameworkSagaRepositoryConfigurator<TSaga>>? configure = null,
        Action<EntityTypeBuilder<TSaga>>? configureSagaMapping = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(sagaRepository);

        return configurator.EntityFrameworkRepository(sagaRepository, configure, new ActionSagaClassMap<TSaga>(configureSagaMapping));
    }

    /// <summary>Registers the saga in a shared EF Core repository using an explicit saga mapping.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="configurator">The saga registration to associate with the shared EF Core repository.</param>
    /// <param name="sagaRepository">The shared mapping and DbContext repository.</param>
    /// <param name="configure">An optional callback that configures concurrency and querying.</param>
    /// <param name="sagaClassMap">The mapping to add, or <see langword="null"/> to use the default saga mapping.</param>
    /// <returns>The same saga registration configurator.</returns>
    public static ISagaRegistrationConfigurator<TSaga> EntityFrameworkRepository<TSaga>(this ISagaRegistrationConfigurator<TSaga> configurator,
        IEntityFrameworkSagaRepository sagaRepository, Action<IEntityFrameworkSagaRepositoryConfigurator<TSaga>>? configure = null,
        ISagaClassMap<TSaga>? sagaClassMap = null)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(sagaRepository);

        var repositoryConfigurator = new EntityFrameworkSagaRepositoryConfigurator<TSaga>();
        repositoryConfigurator.UseDbContextFactory(sagaRepository.CreateDbContext);
        configure?.Invoke(repositoryConfigurator);

        repositoryConfigurator.Validate().ThrowIfContainsFailure("The Entity Framework saga repository configuration is invalid:");

        sagaRepository.AddSagaClassMap(sagaClassMap ?? new ActionSagaClassMap<TSaga>());
        configurator.Repository(x => repositoryConfigurator.Register(x));

        return configurator;
    }

    /// <summary>Configures all job-service saga state machines to use EF Core repositories.</summary>
    /// <param name="configurator">The job-saga registration that receives the repository provider.</param>
    /// <param name="configure">An optional callback applied to each job-service saga repository.</param>
    /// <returns>The same job saga registration configurator.</returns>
    public static IJobSagaRegistrationConfigurator EntityFrameworkRepository(this IJobSagaRegistrationConfigurator configurator,
        Action<IEntityFrameworkSagaRepositoryConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var registrationProvider = new EntityFrameworkSagaRepositoryRegistrationProvider(configure);

        configurator.UseRepositoryRegistrationProvider(registrationProvider);

        return configurator;
    }

    /// <summary>Uses EF Core for saga types registered without a saga-specific repository call.</summary>
    /// <param name="configurator">The registration configurator that receives the default saga repository provider.</param>
    /// <param name="configure">The callback applied to each discovered saga repository.</param>
    public static void SetEntityFrameworkSagaRepositoryProvider(this IRegistrationConfigurator configurator,
        Action<IEntityFrameworkSagaRepositoryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(configure);

        configurator.SetSagaRepositoryProvider(new EntityFrameworkSagaRepositoryRegistrationProvider(configure));
    }

    /// <summary>Selects SQL Server locking for the saga repository.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="configurator">The typed repository configuration on which SQL Server locking is selected.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator<T> UseSqlServer<T>(this IEntityFrameworkSagaRepositoryConfigurator<T> configurator)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.LockStatementProvider = new SqlServerLockStatementProvider();

        return configurator;
    }

    /// <summary>Selects SQL Server locking and a fallback schema for the saga repository.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="configurator">The typed repository configuration on which SQL Server locking is selected.</param>
    /// <param name="schemaName">The schema name to use if the table schema cannot be discovered.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator<T> UseSqlServer<T>(this IEntityFrameworkSagaRepositoryConfigurator<T> configurator,
        string schemaName)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        configurator.LockStatementProvider = new SqlServerLockStatementProvider(schemaName);

        return configurator;
    }

    /// <summary>Selects SQL Server locking for a non-generic saga repository configuration.</summary>
    /// <param name="configurator">The repository configuration on which SQL Server locking is selected.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator UseSqlServer(this IEntityFrameworkSagaRepositoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.LockStatementProvider = new SqlServerLockStatementProvider();

        return configurator;
    }

    /// <summary>Selects SQL Server locking and a fallback schema for a non-generic saga repository configuration.</summary>
    /// <param name="configurator">The repository configuration on which SQL Server locking is selected.</param>
    /// <param name="schemaName">The schema name to use if the table schema cannot be discovered.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator UseSqlServer(this IEntityFrameworkSagaRepositoryConfigurator configurator,
        string schemaName)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        configurator.LockStatementProvider = new SqlServerLockStatementProvider(schemaName);

        return configurator;
    }

    /// <summary>Selects PostgreSQL row locking for the saga repository.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="configurator">The typed repository configuration on which PostgreSQL locking is selected.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator<T> UsePostgreSql<T>(this IEntityFrameworkSagaRepositoryConfigurator<T> configurator)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.LockStatementProvider = new PostgreSqlLockStatementProvider();

        return configurator;
    }

    /// <summary>Selects PostgreSQL row locking and a fallback schema for the saga repository.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="configurator">The typed repository configuration on which PostgreSQL locking is selected.</param>
    /// <param name="schemaName">The schema name to use if the table schema cannot be discovered.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator<T> UsePostgreSql<T>(this IEntityFrameworkSagaRepositoryConfigurator<T> configurator,
        string schemaName)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        configurator.LockStatementProvider = new PostgreSqlLockStatementProvider(schemaName);

        return configurator;
    }

    /// <summary>Selects PostgreSQL row locking for a non-generic saga repository configuration.</summary>
    /// <param name="configurator">The repository configuration on which PostgreSQL locking is selected.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator UsePostgreSql(this IEntityFrameworkSagaRepositoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.LockStatementProvider = new PostgreSqlLockStatementProvider();

        return configurator;
    }

    /// <summary>Selects PostgreSQL row locking and a fallback schema for a non-generic saga repository configuration.</summary>
    /// <param name="configurator">The repository configuration on which PostgreSQL locking is selected.</param>
    /// <param name="schemaName">The schema name to use if the table schema cannot be discovered.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator UsePostgreSql(this IEntityFrameworkSagaRepositoryConfigurator configurator,
        string schemaName)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaName);

        configurator.LockStatementProvider = new PostgreSqlLockStatementProvider(schemaName);

        return configurator;
    }

    /// <summary>
    /// Configures the repository for SQLite. SQLite has no row-level locking, so saga
    /// concurrency is configured as optimistic and must use an application-managed concurrency token.
    /// </summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="configurator">The typed repository configuration switched to optimistic concurrency.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator<T> UseSqlite<T>(this IEntityFrameworkSagaRepositoryConfigurator<T> configurator)
        where T : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.SetOptimisticConcurrency();

        return configurator;
    }

    /// <summary>
    /// Configures the repository for SQLite. SQLite has no row-level locking, so saga
    /// concurrency is configured as optimistic and must use an application-managed concurrency token.
    /// </summary>
    /// <param name="configurator">The repository configuration switched to optimistic concurrency.</param>
    /// <returns>The same repository configurator.</returns>
    public static IEntityFrameworkSagaRepositoryConfigurator UseSqlite(this IEntityFrameworkSagaRepositoryConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.SetOptimisticConcurrency();

        return configurator;
    }

    sealed class ActionSagaClassMap<T> : SagaClassMap<T>
        where T : class, ISaga
    {
        readonly Action<EntityTypeBuilder<T>>? _configure;

        public ActionSagaClassMap(Action<EntityTypeBuilder<T>>? configure = null)
        {
            _configure = configure;
        }

        protected override void Configure(EntityTypeBuilder<T> entity, ModelBuilder model)
        {
            base.Configure(entity, model);
            _configure?.Invoke(entity);
        }
    }
}
