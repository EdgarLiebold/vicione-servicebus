namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Outbox;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class EntityFrameworkBusOutboxRegistrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-REGISTRATION", "default-dbcontext-selection-is-order-independent")]
    public async Task MultipleDbContexts_SelectTheExplicitDefaultIndependentOfRegistrationOrder(bool reverse)
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
    public async Task MultipleDbContexts_WithoutADefaultFailAtScopedResolution()
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
    public async Task TwoBusesSharingOneDbContext_PersistDistinctBusOwnedOutboxes()
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
        services.AddViciOneServiceBus<ISecondaryBus>(configuration =>
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

        await primary.AddSend(CreateSendContext(Guid.NewGuid(), 1));
        await secondary.AddSend(CreateSendContext(Guid.NewGuid(), 2));
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
        Assert.Contains(states, x => x.BusKey == EntityFrameworkBusOutboxIdentity<IBus>.BusKey);
        Assert.Contains(states, x => x.BusKey == EntityFrameworkBusOutboxIdentity<ISecondaryBus>.BusKey);
        Assert.Equal(states.Select(x => x.OutboxId).Order(), messages.Select(x => x.OutboxId!.Value).Order());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-REGISTRATION", "untyped-scope-disposal-fails-before-dbcontext-disposal")]
    public async Task UntypedScopedResolution_DisposeWithoutCommitFailsWithTheTransactionalInvariant()
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
        await context.AddSend(CreateSendContext(Guid.NewGuid(), 1));

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
}
