using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore.Saga;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transactions;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Configuration;

public sealed class EntityFrameworkProviderConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-PROVIDER-CONFIGURATION", "retained-provider-selection")]
    public void ProviderExtensions_SelectOnlyTheRetainedProviderContracts()
    {
        var outbox = new RecordingOutboxConfigurator();
        var saga = new RecordingSagaConfigurator();

        outbox.UseSqlServer();
        Assert.IsType<SqlServerLockStatementProvider>(outbox.Provider);
        outbox.UsePostgres();
        Assert.IsType<PostgresLockStatementProvider>(outbox.Provider);
        Assert.Equal(IsolationLevel.ReadCommitted, outbox.Isolation);
        outbox.UseSqlite();
        Assert.IsType<SqliteLockStatementProvider>(outbox.Provider);
        Assert.Equal(IsolationLevel.Serializable, outbox.Isolation);

        saga.UseSqlite();
        Assert.True(saga.OptimisticConcurrencySelected);
        Assert.True(saga.UseTransaction);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-PROVIDER-CONFIGURATION", "no-retired-provider-or-cache-switch-api")]
    public void PublicApi_ContainsNoRetiredProviderOrSchemaCacheSwitch()
    {
        Assembly product = typeof(SqlLockStatementProvider).Assembly;
        string[] publicTypes = product.GetExportedTypes().Select(type => type.FullName!).ToArray();
        MethodInfo[] providerExtensions = typeof(EntityFrameworkOutboxConfigurationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static);

        Assert.DoesNotContain(publicTypes, name => name.Contains("MySql", StringComparison.Ordinal));
        Assert.DoesNotContain(publicTypes, name => name.Contains("Oracle", StringComparison.Ordinal));
        Assert.DoesNotContain(providerExtensions, method => method.Name is "UseMySql" or "UseOracle");
        Assert.DoesNotContain(providerExtensions, method =>
            method.GetParameters().Any(parameter => parameter.ParameterType == typeof(bool)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-PROVIDER-CONFIGURATION", "pessimistic-provider-is-explicit")]
    public void PessimisticSagaConfiguration_RequiresAnExplicitProvider()
    {
        var configurator = new EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga>();
        configurator.ExistingDbContext<ConfigurationDbContext>();

        ValidationResult failure = Assert.Single(configurator.Validate());

        Assert.Equal(ValidationResultDisposition.Failure, failure.Disposition);
        Assert.Equal("LockStatementProvider", failure.Key);
        Assert.Contains("explicitly", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-PROVIDER-CONFIGURATION", "sqlite-selects-optimistic-saga-concurrency")]
    public void SqliteSagaConfiguration_DoesNotClaimPessimisticRowLocking()
    {
        var configurator = new EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga>();
        configurator.ExistingDbContext<ConfigurationDbContext>();

        configurator.UseSqlite();

        Assert.Empty(configurator.Validate());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONFIGURATION", "final-optimistic-runtime-strategy")]
    public void SagaRegistration_FreezesTheFinalOptimisticStrategy()
    {
        EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga>? captured = null;
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
            configuration.AddSaga<ConfigurationSaga>().EntityFrameworkRepository(repository =>
            {
                var concrete = Assert.IsType<EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga>>(repository);
                concrete.ExistingDbContext<ConfigurationDbContext>();
                concrete.SetOptimisticConcurrency(true);
                concrete.SetOptimisticConcurrency(false);
                concrete.IsolationLevel = IsolationLevel.RepeatableRead;
                captured = concrete;
            }));

        EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga> registered = captured
            ?? throw new InvalidOperationException("The saga repository callback did not expose its configurator.");
        registered.SetOptimisticConcurrency(true);
        registered.IsolationLevel = IsolationLevel.Serializable;

        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaRepositoryLockStrategy<ConfigurationSaga> strategy = provider
            .GetRequiredService<ISagaRepositoryLockStrategy<ConfigurationSaga>>();
        var optimistic = Assert.IsType<OptimisticSagaRepositoryLockStrategy<ConfigurationSaga>>(strategy);

        Assert.False(optimistic.IsTransactionEnabled);
        Assert.Equal(IsolationLevel.RepeatableRead, optimistic.IsolationLevel);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONFIGURATION", "optimistic-api-paths-share-read-committed-default")]
    public void SetOptimisticConcurrency_UsesTheSameReadCommittedDefaultAsTheModeProperty()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
            configuration.AddSaga<ConfigurationSaga>().EntityFrameworkRepository(repository =>
            {
                var concrete = Assert.IsType<EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga>>(repository);
                concrete.ExistingDbContext<ConfigurationDbContext>();
                concrete.SetOptimisticConcurrency();
            }));

        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaRepositoryLockStrategy<ConfigurationSaga> strategy = provider
            .GetRequiredService<ISagaRepositoryLockStrategy<ConfigurationSaga>>();
        var optimistic = Assert.IsType<OptimisticSagaRepositoryLockStrategy<ConfigurationSaga>>(strategy);

        Assert.True(optimistic.IsTransactionEnabled);
        Assert.Equal(IsolationLevel.ReadCommitted, optimistic.IsolationLevel);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONFIGURATION", "explicit-pessimistic-runtime-strategy")]
    public void SagaRegistration_ResolvesAnExplicitPessimisticStrategy()
    {
        EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga>? captured = null;
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
            configuration.AddSaga<ConfigurationSaga>().EntityFrameworkRepository(repository =>
            {
                var concrete = Assert.IsType<EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga>>(repository);
                concrete.ExistingDbContext<ConfigurationDbContext>();
                concrete.UsePostgres();
                concrete.IsolationLevel = IsolationLevel.ReadCommitted;
                concrete.CustomizeQuery(query => query.Where(saga => saga.CorrelationId != Guid.Empty));
                captured = concrete;
            }));

        EntityFrameworkSagaRepositoryConfigurator<ConfigurationSaga> registered = captured
            ?? throw new InvalidOperationException("The saga repository callback did not expose its configurator.");
        registered.UseSqlServer();
        registered.IsolationLevel = IsolationLevel.ReadUncommitted;

        using ServiceProvider provider = services.BuildServiceProvider();
        ISagaRepositoryLockStrategy<ConfigurationSaga> strategy = provider
            .GetRequiredService<ISagaRepositoryLockStrategy<ConfigurationSaga>>();
        var pessimistic = Assert.IsType<PessimisticSagaRepositoryLockStrategy<ConfigurationSaga>>(strategy);

        Assert.True(pessimistic.IsTransactionEnabled);
        Assert.Equal(IsolationLevel.ReadCommitted, pessimistic.IsolationLevel);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-SAGA-CONFIGURATION", "invalid-query-customization-fails-closed")]
    public void SagaStrategy_RejectsANullQueryCustomizationResult()
    {
        var executor = new OptimisticLoadQueryExecutor<ConfigurationSaga>(_ => null!);
        var strategy = new OptimisticSagaRepositoryLockStrategy<ConfigurationSaga>(
            executor,
            _ => null!,
            IsolationLevel.ReadCommitted,
            isTransactionEnabled: true);
        IQueryable<ConfigurationSaga> source = Array.Empty<ConfigurationSaga>().AsQueryable();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            strategy.ApplyQueryCustomization(source));

        Assert.Equal("The saga query customization returned null.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "explicit-provider-required")]
    public void OutboxConfiguration_RejectsAnImplicitProvider()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBus(configuration =>
                configuration.AddEntityFrameworkOutbox<ConfigurationDbContext>()));

        Assert.Contains("provider", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("explicitly", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "lightweight-bus-capabilities-are-mutually-exclusive")]
    public void BusOutboxAndLightweightBusCapabilities_RejectMixedScopedOwnership(
        bool ambientCapability,
        bool busOutboxFirst)
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBus(configuration =>
            {
                if (busOutboxFirst)
                    AddBusOutbox(configuration);

                if (ambientCapability)
                    configuration.AddAmbientTransactionBus();
                else
                    configuration.AddBufferedBus();

                if (!busOutboxFirst)
                    AddBusOutbox(configuration);
            }));

        Assert.Contains("cannot", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(busOutboxFirst ? "EntityFramework" : "Entity Framework", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            busOutboxFirst
                ? ambientCapability
                    ? nameof(DependencyInjectionTransactionExtensions.AddAmbientTransactionBus)
                    : nameof(DependencyInjectionTransactionExtensions.AddBufferedBus)
                : ambientCapability
                    ? "AmbientTransaction"
                    : "BufferedBus",
            exception.Message,
            StringComparison.Ordinal);

        static void AddBusOutbox(IBusRegistrationConfigurator configuration)
        {
            configuration.AddEntityFrameworkOutbox<ConfigurationDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.UseBusOutbox(busOutbox => busOutbox.DisableDeliveryService());
            });
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "registration-freezes-runtime-settings")]
    public void OutboxConfiguration_FreezesValuesAtRegistrationCompletion()
    {
        IEntityFrameworkOutboxConfigurator? capturedOutbox = null;
        IEntityFrameworkBusOutboxConfigurator? capturedBusOutbox = null;
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
            configuration.AddEntityFrameworkOutbox<ConfigurationDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.DuplicateDetectionWindow = TimeSpan.FromMinutes(17);
                outbox.QueryDelay = TimeSpan.FromSeconds(3);
                outbox.QueryMessageLimit = 41;
                outbox.QueryTimeout = TimeSpan.FromSeconds(7);
                outbox.UseBusOutbox(busOutbox =>
                {
                    busOutbox.MessageDeliveryLimit = 13;
                    busOutbox.MessageDeliveryTimeout = TimeSpan.FromSeconds(11);
                    busOutbox.MaximumDeliveryAttempts = 7;
                    busOutbox.InitialDeliveryRetryDelay = TimeSpan.FromSeconds(2);
                    busOutbox.MaximumDeliveryRetryDelay = TimeSpan.FromSeconds(19);
                    capturedBusOutbox = busOutbox;
                });
                capturedOutbox = outbox;
            }));

        IEntityFrameworkOutboxConfigurator registeredOutbox = capturedOutbox
            ?? throw new InvalidOperationException("The registration callback did not expose the outbox configurator.");
        IEntityFrameworkBusOutboxConfigurator registeredBusOutbox = capturedBusOutbox
            ?? throw new InvalidOperationException("The registration callback did not expose the bus-outbox configurator.");
        registeredOutbox.UseSqlServer();
        registeredOutbox.DuplicateDetectionWindow = TimeSpan.FromHours(1);
        registeredOutbox.QueryDelay = TimeSpan.FromMinutes(1);
        registeredOutbox.QueryMessageLimit = 999;
        registeredOutbox.QueryTimeout = TimeSpan.FromMinutes(2);
        registeredBusOutbox.MessageDeliveryLimit = 777;
        registeredBusOutbox.MessageDeliveryTimeout = TimeSpan.FromMinutes(3);
        registeredBusOutbox.MaximumDeliveryAttempts = 99;
        registeredBusOutbox.InitialDeliveryRetryDelay = TimeSpan.FromMinutes(4);
        registeredBusOutbox.MaximumDeliveryRetryDelay = TimeSpan.FromMinutes(5);

        using ServiceProvider provider = services.BuildServiceProvider();
        EntityFrameworkOutboxOptions<ConfigurationDbContext> outboxOptions = provider
            .GetRequiredService<IOptions<EntityFrameworkOutboxOptions<ConfigurationDbContext>>>().Value;
        InboxCleanupServiceOptions<ConfigurationDbContext> cleanupOptions = provider
            .GetRequiredService<IOptions<InboxCleanupServiceOptions<ConfigurationDbContext>>>().Value;
        OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, ConfigurationDbContext>> deliveryOptions = provider
            .GetRequiredService<IOptions<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, ConfigurationDbContext>>>>().Value;

        Assert.IsType<PostgresLockStatementProvider>(outboxOptions.LockStatementProvider);
        Assert.Equal(TimeSpan.FromMinutes(17), cleanupOptions.DuplicateDetectionWindow);
        Assert.Equal(TimeSpan.FromSeconds(3), cleanupOptions.QueryDelay);
        Assert.Equal(41, cleanupOptions.QueryMessageLimit);
        Assert.Equal(TimeSpan.FromSeconds(7), cleanupOptions.QueryTimeout);
        Assert.Equal(13, deliveryOptions.MessageDeliveryLimit);
        Assert.Equal(TimeSpan.FromSeconds(11), deliveryOptions.MessageDeliveryTimeout);
        Assert.Equal(7, deliveryOptions.MaximumDeliveryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(2), deliveryOptions.InitialDeliveryRetryDelay);
        Assert.Equal(TimeSpan.FromSeconds(19), deliveryOptions.MaximumDeliveryRetryDelay);
    }

    [Theory]
    [InlineData(InvalidDeliverySetting.MessageLimit, "MessageDeliveryLimit")]
    [InlineData(InvalidDeliverySetting.MessageTimeout, "MessageDeliveryTimeout")]
    [InlineData(InvalidDeliverySetting.AttemptLimit, "MaximumDeliveryAttempts")]
    [InlineData(InvalidDeliverySetting.InitialRetryDelay, "InitialDeliveryRetryDelay")]
    [InlineData(InvalidDeliverySetting.MaximumRetryDelay, "MaximumDeliveryRetryDelay")]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "invalid-delivery-settings-fail-at-registration")]
    public void BusOutboxConfiguration_RejectsEveryInvalidReliabilityBoundary(
        InvalidDeliverySetting setting,
        string expectedSetting)
    {
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBus(configuration =>
                configuration.AddEntityFrameworkOutbox<ConfigurationDbContext>(outbox =>
                {
                    outbox.UseSqlite();
                    outbox.UseBusOutbox(busOutbox =>
                    {
                        switch (setting)
                        {
                            case InvalidDeliverySetting.MessageLimit:
                                busOutbox.MessageDeliveryLimit = 0;
                                break;
                            case InvalidDeliverySetting.MessageTimeout:
                                busOutbox.MessageDeliveryTimeout = TimeSpan.Zero;
                                break;
                            case InvalidDeliverySetting.AttemptLimit:
                                busOutbox.MaximumDeliveryAttempts = 0;
                                break;
                            case InvalidDeliverySetting.InitialRetryDelay:
                                busOutbox.InitialDeliveryRetryDelay = TimeSpan.Zero;
                                break;
                            case InvalidDeliverySetting.MaximumRetryDelay:
                                busOutbox.InitialDeliveryRetryDelay = TimeSpan.FromSeconds(2);
                                busOutbox.MaximumDeliveryRetryDelay = TimeSpan.FromSeconds(1);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(setting), setting, null);
                        }
                    });
                })));

        Assert.Contains(expectedSetting, failure.Message, StringComparison.Ordinal);
    }

    public enum InvalidDeliverySetting
    {
        MessageLimit,
        MessageTimeout,
        AttemptLimit,
        InitialRetryDelay,
        MaximumRetryDelay
    }

    public sealed class ConfigurationSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : DbContext(options)
    {
        public ConfigurationDbContext() : this(new DbContextOptionsBuilder<ConfigurationDbContext>().Options)
        {
        }
    }

    private sealed class RecordingOutboxConfigurator : IEntityFrameworkOutboxConfigurator
    {
        public ILockStatementProvider? Provider { get; private set; }
        public IsolationLevel Isolation { get; private set; }

        public TimeSpan DuplicateDetectionWindow { private get; set; }
        public IsolationLevel IsolationLevel { set => Isolation = value; }
        public ILockStatementProvider LockStatementProvider { set => Provider = value; }
        public TimeSpan QueryDelay { get; set; }
        public int QueryMessageLimit { get; set; }
        public TimeSpan QueryTimeout { get; set; }
        public void DisableInboxCleanupService() => throw new NotSupportedException();
        public void UseBusOutbox(Action<IEntityFrameworkBusOutboxConfigurator>? configure = null) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingSagaConfigurator : IEntityFrameworkSagaRepositoryConfigurator
    {
        public bool OptimisticConcurrencySelected { get; private set; }
        public bool UseTransaction { get; private set; }

        public ConcurrencyMode ConcurrencyMode { private get; set; }
        public IsolationLevel IsolationLevel { private get; set; }
        public ILockStatementProvider LockStatementProvider { private get; set; } = null!;
        public void AddDbContext<TContext, TImplementation>(
            Action<IServiceProvider, DbContextOptionsBuilder<TImplementation>>? optionsAction = null)
            where TContext : DbContext
            where TImplementation : DbContext, TContext => throw new NotSupportedException();
        public void DatabaseFactory(Func<DbContext> databaseFactory) => throw new NotSupportedException();
        public void DatabaseFactory(Func<IServiceProvider, Func<DbContext>> databaseFactory) => throw new NotSupportedException();
        public void ExistingDbContext<TContext>() where TContext : DbContext => throw new NotSupportedException();

        public void SetOptimisticConcurrency(bool useTransaction = true)
        {
            OptimisticConcurrencySelected = true;
            UseTransaction = useTransaction;
        }
    }
}
