using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkReliableMessagingRegistrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-REGISTRATION", "one-store-instance-and-one-delivery-loop-per-bus")]
    public async Task Provider_RegistersOneSharedStoreAndOneDeliveryHostedServicePerBusAsync()
    {
        string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"vicione-reliable-registration-{Guid.NewGuid():N}.db")};Pooling=False";
        try
        {
            await using ServiceProvider provider = BuildProvider(connectionString);

            object outbox = provider.GetRequiredService<IOutboxStore<ISecondaryBus>>();
            object inbox = provider.GetRequiredService<IInboxStore<ISecondaryBus>>();
            object schedules = provider.GetRequiredService<IScheduleStore<ISecondaryBus>>();
            Assert.Same(outbox, inbox);
            Assert.Same(outbox, schedules);

            IHostedService[] deliveryServices = provider.GetServices<IHostedService>()
                .Where(service => service.GetType().IsGenericType
                    && service.GetType().GetGenericTypeDefinition().Name == "ReliableMessagingDeliveryService`1"
                    && service.GetType().GetGenericArguments()[0] == typeof(ISecondaryBus))
                .ToArray();
            Assert.Single(deliveryServices);
            Assert.DoesNotContain(provider.GetServices<IHostedService>(), service =>
                service.GetType().Name.Contains("EntityFrameworkTransactionalOutboxSource", StringComparison.Ordinal));
        }
        finally
        {
            DeleteDatabase(connectionString);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PERSISTENCE-IDENTITY", "transactional-and-standalone-sends-share-one-outbox")]
    public async Task TransactionalAndStandaloneSends_PersistToTheSameOutboxAndBusIdentityAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"vicione-shared-outbox-{Guid.NewGuid():N}.db")};Pooling=False";
        try
        {
            await using ServiceProvider provider = BuildProvider(connectionString);
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            ReliableDbContext dbContext = scope.ServiceProvider.GetRequiredService<ReliableDbContext>();
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            var transactional = (EntityFrameworkScopedBusContext<ISecondaryBus, ReliableDbContext>)scope.ServiceProvider
                .GetRequiredService<IEntityFrameworkTransactionalOutbox<ISecondaryBus, ReliableDbContext>>();
            Guid transactionalId = Guid.NewGuid();
            await transactional.AddSendAsync(CreateSendContext(transactionalId, 7), cancellationToken);
            await transactional.CommitAsync(cancellationToken);

            Guid standaloneId = Guid.NewGuid();
            IDurableSender<ISecondaryBus> sender = scope.ServiceProvider.GetRequiredService<IDurableSender<ISecondaryBus>>();
            DurableSendReceipt receipt = await sender.SendAsync(
                new Uri("loopback://orders/registration-probe"),
                new RegistrationProbe(8),
                new DurableSendOptions { IdempotencyKey = new DurableSendId(standaloneId) },
                cancellationToken);
            Assert.True(receipt.IsNew);

            await using ReliableDbContext verification = await provider.GetRequiredService<IDbContextFactory<ReliableDbContext>>()
                .CreateDbContextAsync(cancellationToken);
            DurableSendRecord[] rows = await verification.Set<DurableSendRecord>()
                .AsNoTracking()
                .OrderBy(row => row.Id)
                .ToArrayAsync(cancellationToken);
            Assert.Equal(new[] { standaloneId, transactionalId }.Order(), rows.Select(row => row.Id));
            Assert.All(rows, row => Assert.Equal("orders-v1", row.StoreKey));
            Assert.All(rows, row => Assert.Equal("registration-probe;v=1", row.ContractIdentity));
            Assert.Equal(2, Assert.Single(await verification.Set<DurableSendCapacityState>()
                .AsNoTracking()
                .ToArrayAsync(cancellationToken)).StoredCount);
        }
        finally
        {
            DeleteDatabase(connectionString);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-REGISTRATION", "duplicate-store-selection-fails-at-registration")]
    public void DuplicateStoreSelection_IsRejectedBeforeTheProviderBuilds()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBus<ISecondaryBus>("orders-v1", bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.UsingInMemory((_, transport) => transport.Host(new Uri("loopback://orders/")));
                bus.UseReliableMessaging(reliable =>
                {
                    reliable.UseEntityFramework<ReliableDbContext>();
                    reliable.UseEntityFramework<ReliableDbContext>();
                });
            }));

        Assert.Contains("already has a persistence store", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "reliable-session-is-reused-per-scope")]
    public async Task ReliableFactory_ReusesTheRegistrySessionWithinOneScopeAsync()
    {
        string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"vicione-reliable-session-{Guid.NewGuid():N}.db")};Pooling=False";
        try
        {
            await using ServiceProvider provider = BuildProvider(connectionString);
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            EntityFrameworkBusOutboxSessionRegistry<ISecondaryBus> registry = scope.ServiceProvider
                .GetRequiredService<EntityFrameworkBusOutboxSessionRegistry<ISecondaryBus>>();
            EntityFrameworkScopedBusContext<ISecondaryBus, ReliableDbContext> first = registry
                .GetOrCreate<ReliableDbContext>(scope.ServiceProvider);
            EntityFrameworkScopedBusContext<ISecondaryBus, ReliableDbContext> second = registry
                .GetOrCreate<ReliableDbContext>(scope.ServiceProvider);
            IEntityFrameworkScopedBusContextFactory<ISecondaryBus> factory = Assert.Single(
                provider.GetServices<IEntityFrameworkScopedBusContextFactory<ISecondaryBus>>());

            Assert.Same(first, second);
            Assert.Same(first, factory.Create(scope.ServiceProvider));
            Assert.Same(first, scope.ServiceProvider
                .GetRequiredService<IEntityFrameworkTransactionalOutbox<ISecondaryBus, ReliableDbContext>>());
        }
        finally
        {
            DeleteDatabase(connectionString);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "concurrent-reliable-session-resolution-has-one-winner")]
    public async Task ReliableRegistry_ConcurrentResolutionReturnsOneSessionAsync()
    {
        string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"vicione-reliable-concurrent-session-{Guid.NewGuid():N}.db")};Pooling=False";
        try
        {
            await using ServiceProvider provider = BuildProvider(connectionString);
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            EntityFrameworkBusOutboxSessionRegistry<ISecondaryBus> registry = scope.ServiceProvider
                .GetRequiredService<EntityFrameworkBusOutboxSessionRegistry<ISecondaryBus>>();
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<EntityFrameworkScopedBusContext<ISecondaryBus, ReliableDbContext>>[] resolutions = Enumerable
                .Range(0, 16)
                .Select(async _ =>
                {
                    await release.Task;
                    return registry.GetOrCreate<ReliableDbContext>(scope.ServiceProvider);
                })
                .ToArray();

            release.SetResult();
            EntityFrameworkScopedBusContext<ISecondaryBus, ReliableDbContext>[] contexts =
                await Task.WhenAll(resolutions);

            EntityFrameworkScopedBusContext<ISecondaryBus, ReliableDbContext> expected = Assert.Single(
                contexts.Distinct());
            Assert.All(contexts, context => Assert.Same(expected, context));
        }
        finally
        {
            DeleteDatabase(connectionString);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "reliable-factory-preserves-ambient-consume-context")]
    public async Task ReliableFactory_PreservesAnAmbientConsumeContextAsync()
    {
        string connectionString = $"Data Source={Path.Combine(Path.GetTempPath(), $"vicione-reliable-ambient-{Guid.NewGuid():N}.db")};Pooling=False";
        try
        {
            await using ServiceProvider provider = BuildProvider(connectionString);
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEntityFrameworkScopedBusContextFactory<ISecondaryBus> factory = Assert.Single(
                provider.GetServices<IEntityFrameworkScopedBusContextFactory<ISecondaryBus>>());
            IScopedConsumeContextProvider contextProvider = scope.ServiceProvider
                .GetRequiredService<Bind<ISecondaryBus, IScopedConsumeContextProvider>>()
                .Value;
            ConsumeContext ambient = DispatchProxy.Create<ConsumeContext, PassiveConsumeContextProxy>();

            using (contextProvider.PushContext(ambient))
            {
                Assert.IsType<ConsumeContextScopedBusContext>(factory.Create(scope.ServiceProvider));
                using EntityFrameworkScopedBusContext<ISecondaryBus, ReliableDbContext> explicitContext =
                    EntityFrameworkScopedBusContextFactory<ISecondaryBus, ReliableDbContext>
                        .CreateTransactionalContext(scope.ServiceProvider);
                Assert.IsType<EntityFrameworkConsumeContextScopedBusContext<ISecondaryBus, ReliableDbContext>>(
                    explicitContext);
            }
        }
        finally
        {
            DeleteDatabase(connectionString);
        }
    }

    private static ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPooledDbContextFactory<ReliableDbContext>(options => options.UseSqlite(connectionString));
        services.AddViciOneServiceBus<ISecondaryBus>("orders-v1", bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory((_, transport) => transport.Host(new Uri("loopback://orders/")));
            bus.UseReliableMessaging(reliable =>
            {
                reliable.UseEntityFramework<ReliableDbContext>();
                reliable.Store(new ReliableStoreLimits
                {
                    MaximumStoredCount = 100,
                    MaximumStoredBytes = 1024 * 1024,
                });
                reliable.Delivery(delivery =>
                {
                    delivery.MaximumConcurrentDeliveries = 4;
                    delivery.PollInterval = TimeSpan.FromMilliseconds(25);
                });
                reliable.Retention(TimeSpan.FromDays(7));
                reliable.AddMessageContract<RegistrationProbe>("registration-probe");
            });
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private static MessageSendContext<RegistrationProbe> CreateSendContext(Guid messageId, int sequence) => new(
        new RegistrationProbe(sequence))
    {
        MessageId = messageId,
        Serializer = ServiceBusMetadataJson.MessageSerializer,
        DestinationAddress = new Uri("loopback://orders/registration-probe"),
    };

    private static void DeleteDatabase(string connectionString)
    {
        string path = new SqliteConnectionStringBuilder(connectionString).DataSource;
        File.Delete(path);
        File.Delete(path + "-wal");
        File.Delete(path + "-shm");
    }

    public interface ISecondaryBus : IBus;

    private sealed record RegistrationProbe(int Sequence);

    private sealed class ReliableDbContext(DbContextOptions<ReliableDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddViciOneReliableMessaging();
    }

    private class PassiveConsumeContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }
}
