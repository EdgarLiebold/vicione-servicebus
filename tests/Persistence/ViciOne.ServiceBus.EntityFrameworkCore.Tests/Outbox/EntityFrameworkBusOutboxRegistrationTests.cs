using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkBusOutboxRegistrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-REGISTRATION", "default-dbcontext-selection-is-order-independent")]
    public async Task MultipleDbContexts_SelectTheExplicitDefaultIndependentOfRegistrationOrderAsync(bool reverse)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using ServiceProvider provider = BuildDefaultBus(connection, reverse, selectDefault: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        ScopedBusContext selected = scope.ServiceProvider.GetRequiredService<IScopedBusContextProvider<IBus>>().Context;

        Assert.IsType<EntityFrameworkScopedBusContext<IBus, SecondDbContext>>(selected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-REGISTRATION", "ambiguous-dbcontext-selection-fails-closed")]
    public async Task MultipleDbContexts_WithoutADefaultFailAtScopedResolutionAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using ServiceProvider provider = BuildDefaultBus(connection, reverse: false, selectDefault: false);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            scope.ServiceProvider.GetRequiredService<IScopedBusContextProvider<IBus>>());

        Assert.Contains("explicit default DbContext", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-REGISTRATION", "duplicate-default-is-rejected-during-configuration")]
    public void MultipleDefaults_AreRejectedDuringConfiguration()
    {
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBus(configuration =>
            {
                AddOutbox<FirstDbContext>(configuration, isDefault: true);
                AddOutbox<SecondDbContext>(configuration, isDefault: true);
            }));

        Assert.Contains("default Entity Framework bus outbox is already configured", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-REGISTRATION", "duplicate-bus-dbcontext-pair-is-rejected")]
    public void DuplicateBusAndDbContextPair_IsRejectedDuringConfiguration()
    {
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBus(configuration =>
            {
                AddOutbox<FirstDbContext>(configuration, isDefault: false);
                AddOutbox<FirstDbContext>(configuration, isDefault: false);
            }));

        Assert.Contains("already configured", failure.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(FirstDbContext), failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-REGISTRATION", "same-dbcontext-is-isolated-by-bus")]
    public async Task TwoBusesSharingOneDbContext_PersistDistinctBusOwnedOutboxesAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<SharedDbContext>(options => options.UseSqlite(connection));
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.AddEntityFrameworkOutbox<SharedDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.DisableInboxCleanupService();
                outbox.UseBusOutbox(busOutbox => busOutbox.DisableDeliveryService());
            });
            configuration.UsingInMemory((_, _) => { });
        });
        services.AddViciOneServiceBus<ISecondaryBus>("secondary-v1", configuration =>
        {
            configuration.AddEntityFrameworkOutbox<ISecondaryBus, SharedDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.DisableInboxCleanupService();
                outbox.UseBusOutbox(busOutbox => busOutbox.DisableDeliveryService());
            });
            configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://secondary/")));
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        var primary = (EntityFrameworkScopedBusContext<IBus, SharedDbContext>)scope.ServiceProvider
            .GetRequiredService<IEntityFrameworkTransactionalOutbox<IBus, SharedDbContext>>();
        var secondary = (EntityFrameworkScopedBusContext<ISecondaryBus, SharedDbContext>)scope.ServiceProvider
            .GetRequiredService<IEntityFrameworkTransactionalOutbox<ISecondaryBus, SharedDbContext>>();
        SharedDbContext dbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await primary.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        await secondary.AddSendAsync(CreateSendContext(Guid.NewGuid(), 2), TestContext.Current.CancellationToken);
        Assert.Equal(2, dbContext.Set<OutboxState>().Local.Select(x => x.BusKey).Distinct(StringComparer.Ordinal).Count());

        await primary.CommitAsync(TestContext.Current.CancellationToken);
        await secondary.CommitAsync(TestContext.Current.CancellationToken);
        Assert.False(primary.HasActiveSession);
        Assert.False(secondary.HasActiveSession);
        Assert.Same(primary, scope.ServiceProvider.GetRequiredService<EntityFrameworkScopedBusContext<IBus, SharedDbContext>>());
        Assert.Same(secondary,
            scope.ServiceProvider.GetRequiredService<EntityFrameworkScopedBusContext<ISecondaryBus, SharedDbContext>>());
        dbContext.ChangeTracker.Clear();

        OutboxState[] states = await dbContext.Set<OutboxState>().OrderBy(x => x.BusKey)
            .ToArrayAsync(TestContext.Current.CancellationToken);
        OutboxMessage[] messages = await dbContext.Set<OutboxMessage>().OrderBy(x => x.SequenceNumber)
            .ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, states.Length);
        Assert.Equal(2, states.Select(x => x.BusKey).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(states, x => x.BusKey == "default");
        Assert.Contains(states, x => x.BusKey == "secondary-v1");
        Assert.Equal(states.Select(x => x.OutboxId).Order(), messages.Select(x => x.OutboxId!.Value).Order());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PERSISTENCE-IDENTITY", "typed-bus-outbox-and-durable-sender-persist-one-shared-identity")]
    public async Task TypedBusOutboxAndDurableSender_PersistTheSameConfiguredBusIdentityAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string path = Path.Combine(Path.GetTempPath(), $"vicione-persistence-identity-{Guid.NewGuid():N}.db");
        string connectionString = $"Data Source={path};Pooling=False";
        try
        {
            await using (var setup = new SqliteConnection(connectionString))
            {
                await setup.OpenAsync(cancellationToken);
                await using SqliteCommand command = setup.CreateCommand();
                command.CommandText = "PRAGMA journal_mode=WAL;";
                Assert.Equal("wal", Convert.ToString(await command.ExecuteScalarAsync(cancellationToken))?.ToLowerInvariant());
            }

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddPooledDbContextFactory<CombinedPersistenceDbContext>(options => options.UseSqlite(connectionString));
            services.AddViciOneMessageContracts(catalog => catalog.Register<RegistrationProbe>("registration-probe"));
            services.AddViciOneServiceBus<ISecondaryBus>("orders-v1", configuration =>
            {
                configuration.AddEntityFrameworkOutbox<ISecondaryBus, CombinedPersistenceDbContext>(outbox =>
                {
                    outbox.UseSqlite();
                    outbox.DisableInboxCleanupService();
                    outbox.UseBusOutbox(busOutbox => busOutbox.DisableDeliveryService());
                });
                configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://orders/")));
                configuration.UseDurableSender(durable => durable.UseEntityFramework<CombinedPersistenceDbContext>());
            });

            await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            CombinedPersistenceDbContext dbContext = scope.ServiceProvider.GetRequiredService<CombinedPersistenceDbContext>();
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            var outbox = (EntityFrameworkScopedBusContext<ISecondaryBus, CombinedPersistenceDbContext>)scope.ServiceProvider
                .GetRequiredService<IEntityFrameworkTransactionalOutbox<ISecondaryBus, CombinedPersistenceDbContext>>();
            await outbox.AddSendAsync(CreateSendContext(Guid.NewGuid(), 7), TestContext.Current.CancellationToken);
            await outbox.CommitAsync(cancellationToken);

            IDurableSender<ISecondaryBus> durableSender = scope.ServiceProvider.GetRequiredService<IDurableSender<ISecondaryBus>>();
            DurableSendReceipt receipt = await durableSender.SendAsync(
                new Uri("loopback://orders/registration-probe"),
                new RegistrationProbe(8),
                new DurableSendOptions { IdempotencyKey = new DurableSendId(Guid.NewGuid()) },
                cancellationToken);
            Assert.True(receipt.IsNew);

            await using CombinedPersistenceDbContext verification = await provider
                .GetRequiredService<IDbContextFactory<CombinedPersistenceDbContext>>()
                .CreateDbContextAsync(cancellationToken);
            Assert.Equal("orders-v1", Assert.Single(await verification.Set<OutboxState>()
                .AsNoTracking()
                .ToArrayAsync(cancellationToken)).BusKey);
            Assert.Equal("orders-v1", Assert.Single(await verification.Set<DurableSendRecord>()
                .AsNoTracking()
                .ToArrayAsync(cancellationToken)).StoreKey);
            Assert.Equal("orders-v1", Assert.Single(await verification.Set<DurableSendCapacityState>()
                .AsNoTracking()
                .ToArrayAsync(cancellationToken)).StoreKey);
        }
        finally
        {
            File.Delete(path);
            File.Delete(path + "-wal");
            File.Delete(path + "-shm");
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-REGISTRATION", "untyped-scope-disposal-fails-before-dbcontext-disposal")]
    public async Task UntypedScopedResolution_DisposeWithoutCommitFailsWithTheTransactionalInvariantAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<FirstDbContext>(options => options.UseSqlite(connection));
        services.AddViciOneServiceBus(configuration =>
        {
            AddOutbox<FirstDbContext>(configuration, isDefault: false);
            configuration.UsingInMemory((_, _) => { });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IServiceScope scope = provider.CreateScope();
        var context = (EntityFrameworkScopedBusContext<IBus, FirstDbContext>)scope.ServiceProvider
            .GetRequiredService<IScopedBusContextProvider<IBus>>().Context;
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(scope.Dispose);

        Assert.Contains("disposed without commit", failure.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider BuildDefaultBus(SqliteConnection connection, bool reverse, bool selectDefault)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<FirstDbContext>(options => options.UseSqlite(connection));
        services.AddDbContext<SecondDbContext>(options => options.UseSqlite(connection));
        services.AddViciOneServiceBus(configuration =>
        {
            if (reverse)
            {
                AddOutbox<SecondDbContext>(configuration, selectDefault);
                AddOutbox<FirstDbContext>(configuration, isDefault: false);
            }
            else
            {
                AddOutbox<FirstDbContext>(configuration, isDefault: false);
                AddOutbox<SecondDbContext>(configuration, selectDefault);
            }

            configuration.UsingInMemory((_, _) => { });
        });
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private static void AddOutbox<TDbContext>(IBusRegistrationConfigurator configuration, bool isDefault)
        where TDbContext : DbContext
    {
        configuration.AddEntityFrameworkOutbox<TDbContext>(outbox =>
        {
            outbox.UseSqlite();
            outbox.DisableInboxCleanupService();
            outbox.UseBusOutbox(busOutbox =>
            {
                busOutbox.DisableDeliveryService();
                if (isDefault)
                    busOutbox.UseAsDefault();
            });
        });
    }

    private static MessageSendContext<RegistrationProbe> CreateSendContext(Guid messageId, int sequence) => new(
        new RegistrationProbe(sequence))
    {
        MessageId = messageId,
        Serializer = ServiceBusMetadataJson.MessageSerializer
    };

    public interface ISecondaryBus : IBus;
    private sealed record RegistrationProbe(int Sequence);

    private sealed class FirstDbContext(DbContextOptions<FirstDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    private sealed class SecondDbContext(DbContextOptions<SecondDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    private sealed class SharedDbContext(DbContextOptions<SharedDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    private sealed class CombinedPersistenceDbContext(DbContextOptions<CombinedPersistenceDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
            modelBuilder.AddViciOneDurableSender();
        }
    }
}
