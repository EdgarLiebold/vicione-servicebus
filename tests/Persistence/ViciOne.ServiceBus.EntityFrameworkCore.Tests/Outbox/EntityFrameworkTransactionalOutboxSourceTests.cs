using System.Data;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkTransactionalOutboxSourceTests
{
    private static readonly DateTimeOffset Now = new(2045, 6, 7, 8, 9, 10, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "source-constructor-rejects-invalid-dependencies")]
    public void Constructor_RejectsEveryMissingDependencyAndInvalidPersistenceOption()
    {
        using ServiceProvider provider = CreateBusProvider();
        IOptions<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, SourceDbContext>>> deliveryOptions =
            Options.Create(CreateDeliveryOptions());
        IOptions<EntityFrameworkOutboxOptions<SourceDbContext>> persistenceOptions = Options.Create(CreatePersistenceOptions());
        var notification = new RecordingNotification();
        ITransportSendFailureClassifier[] classifiers = [];
        ILogger<EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>> logger =
            NullLogger<EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>>.Instance;
        var timeProvider = new FakeTimeProvider(Now);
        BusPersistenceIdentity<IBus> identity = BusPersistenceIdentity<IBus>.Create("source");

        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            null!, persistenceOptions, notification, classifiers, logger, provider, timeProvider, identity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, null!, notification, classifiers, logger, provider, timeProvider, identity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, persistenceOptions, null!, classifiers, logger, provider, timeProvider, identity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, persistenceOptions, notification, null!, logger, provider, timeProvider, identity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, persistenceOptions, notification, classifiers, null!, provider, timeProvider, identity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, persistenceOptions, notification, classifiers, logger, null!, timeProvider, identity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, persistenceOptions, notification, classifiers, logger, provider, null!, identity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, persistenceOptions, notification, classifiers, logger, provider, timeProvider, null!));

        var missingLockProvider = Options.Create(new EntityFrameworkOutboxOptions<SourceDbContext>());
        Assert.Throws<ArgumentException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, missingLockProvider, notification, classifiers, logger, provider, timeProvider, identity));

        var undefinedIsolation = Options.Create(new EntityFrameworkOutboxOptions<SourceDbContext>
        {
            IsolationLevel = (IsolationLevel)int.MaxValue,
            LockStatementProvider = new SqliteLockStatementProvider(),
        });
        Assert.Throws<ArgumentOutOfRangeException>(() => new EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>(
            deliveryOptions, undefinedIsolation, notification, classifiers, logger, provider, timeProvider, identity));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "typed-bus-source-resolves-own-control")]
    public void Constructor_ForTypedBusResolvesItsOwningBusControl()
    {
        IBusControl busControl = global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(_ => { });
        var bus = new SecondaryBus(busControl);
        IBusInstance<ISecondaryBus> instance = DispatchProxy.Create<IBusInstance<ISecondaryBus>, TypedBusInstanceProxy>();
        ((TypedBusInstanceProxy)(object)instance).BusControl = busControl;
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<ISecondaryBus>(bus)
            .AddSingleton(instance)
            .BuildServiceProvider();

        var source = new EntityFrameworkTransactionalOutboxSource<ISecondaryBus, SourceDbContext>(
            Options.Create(new OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<ISecondaryBus, SourceDbContext>>()),
            Options.Create(CreatePersistenceOptions()),
            new TypedNotification(),
            [],
            NullLogger<EntityFrameworkTransactionalOutboxSource<ISecondaryBus, SourceDbContext>>.Instance,
            provider,
            new FakeTimeProvider(Now),
            BusPersistenceIdentity<ISecondaryBus>.Create("secondary"));

        Assert.NotNull(source);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "source-wait-delegates-cancellation")]
    public async Task WaitForWork_DelegatesTheExactCancellationTokenAsync()
    {
        using ServiceProvider provider = CreateBusProvider();
        var notification = new RecordingNotification();
        EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext> source = CreateSource(provider, notification);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        Task wait = source.WaitForWorkAsync(cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
        Assert.Equal(cancellation.Token, notification.LastToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "source-cancellation-remains-observable")]
    public async Task DueBatch_CallerCancellationRemainsObservableAsync()
    {
        await using SourceEnvironment environment = await SourceEnvironment.CreateAsync();
        EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext> source = environment.CreateSource();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.DeliverDueBatchAsync(cancellation.Token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-DELIVERY", "paged-source-delivery-persists-progress-and-cleans-up")]
    public async Task PagedDelivery_PersistsProgressCompletesAndCleansTheOutboxAsync()
    {
        await using SourceEnvironment environment = await SourceEnvironment.CreateAsync();
        await environment.SeedPendingOutboxAsync(2);
        EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext> source = environment.CreateSource(
            CreateDeliveryOptions(queryMessageLimit: 1, messageDeliveryLimit: 1));

        Assert.True(await source.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        SourceSnapshot first = await environment.ReadSnapshotAsync();
        Assert.Equal(OutboxDeliveryStatus.Pending, first.State?.Status);
        Assert.NotNull(first.State?.LastSequenceNumber);
        Assert.Equal(1, first.MessageCount);

        Assert.True(await source.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        SourceSnapshot second = await environment.ReadSnapshotAsync();
        Assert.Equal(OutboxDeliveryStatus.Pending, second.State?.Status);
        Assert.True(second.State?.LastSequenceNumber > first.State?.LastSequenceNumber);
        Assert.Equal(0, second.MessageCount);

        Assert.True(await source.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        SourceSnapshot completed = await environment.ReadSnapshotAsync();
        Assert.Equal(OutboxDeliveryStatus.Delivered, completed.State?.Status);
        Assert.Equal(Now.UtcDateTime, completed.State?.Delivered);
        Assert.Equal(0, completed.MessageCount);

        Assert.True(await source.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        SourceSnapshot removed = await environment.ReadSnapshotAsync();
        Assert.Null(removed.State);
        Assert.Equal(0, removed.MessageCount);
        Assert.False(await source.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
    }

    private static EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext> CreateSource(
        IServiceProvider provider,
        RecordingNotification? notification = null,
        OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, SourceDbContext>>? deliveryOptions = null) => new(
            Options.Create(deliveryOptions ?? CreateDeliveryOptions()),
            Options.Create(CreatePersistenceOptions()),
            notification ?? new RecordingNotification(),
            [],
            NullLogger<EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext>>.Instance,
            provider,
            new FakeTimeProvider(Now),
            BusPersistenceIdentity<IBus>.Create("source"));

    private static OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, SourceDbContext>> CreateDeliveryOptions(
        int queryMessageLimit = 100,
        int messageDeliveryLimit = 100) => new()
        {
            QueryDelay = TimeSpan.FromMilliseconds(1),
            QueryMessageLimit = queryMessageLimit,
            QueryTimeout = TimeSpan.FromSeconds(5),
            MessageDeliveryLimit = messageDeliveryLimit,
            MessageDeliveryTimeout = TimeSpan.FromSeconds(5),
            MaximumDeliveryAttempts = 3,
            InitialDeliveryRetryDelay = TimeSpan.FromSeconds(1),
            MaximumDeliveryRetryDelay = TimeSpan.FromSeconds(4),
        };

    private static EntityFrameworkOutboxOptions<SourceDbContext> CreatePersistenceOptions() => new()
    {
        IsolationLevel = IsolationLevel.Serializable,
        LockStatementProvider = new SqliteLockStatementProvider(),
    };

    private static ServiceProvider CreateBusProvider()
    {
        IBusControl bus = global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(_ => { });
        return new ServiceCollection()
            .AddSingleton<IBus>(bus)
            .AddSingleton(bus)
            .BuildServiceProvider();
    }

    private static OutboxMessage CreateMessage(Guid outboxId, int sequence)
    {
        var context = new MessageSendContext<DeliveryProbe>(new DeliveryProbe(sequence))
        {
            MessageId = Guid.NewGuid(),
            DestinationAddress = new Uri("loopback://localhost/ef-transactional-source"),
            Serializer = ServiceBusMetadataJson.MessageSerializer,
        };

        return OutboxMessageFactory.Create(
            context,
            ServiceBusMetadataJson.ObjectDeserializer,
            new FakeTimeProvider(Now),
            outboxId: outboxId);
    }

    private sealed record DeliveryProbe(int Sequence);
    private sealed record SourceSnapshot(OutboxState? State, int MessageCount);
    private interface ISecondaryBus : IBus;
    private sealed class SecondaryBus(IBusControl busControl) : ViciOne.ServiceBus.Advanced.BusInstance<ISecondaryBus>(busControl), ISecondaryBus;

    private class TypedBusInstanceProxy : DispatchProxy
    {
        public IBusControl BusControl { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_BusControl")
                return BusControl;

            throw new InvalidOperationException($"Unexpected typed bus instance member: {targetMethod?.Name ?? "<null>"}.");
        }
    }

    private sealed class TypedNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<ISecondaryBus, SourceDbContext>>
    {
        public Task WaitForDeliveryAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void SignalDelivery()
        {
        }
    }

    private sealed class RecordingNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, SourceDbContext>>
    {
        public CancellationToken LastToken { get; private set; }

        public Task WaitForDeliveryAsync(CancellationToken cancellationToken)
        {
            LastToken = cancellationToken;
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        public void SignalDelivery()
        {
        }
    }

    private sealed class SourceDbContext(DbContextOptions<SourceDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    private sealed class SourceEnvironment : IAsyncDisposable
    {
        private readonly IBusControl _bus;
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<SourceDbContext> _dbContextOptions;
        private readonly ServiceProvider _provider;

        private SourceEnvironment(
            IBusControl bus,
            SqliteConnection connection,
            DbContextOptions<SourceDbContext> dbContextOptions,
            ServiceProvider provider)
        {
            _bus = bus;
            _connection = connection;
            _dbContextOptions = dbContextOptions;
            _provider = provider;
        }

        public static async Task<SourceEnvironment> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            DbContextOptions<SourceDbContext> dbContextOptions =
                new DbContextOptionsBuilder<SourceDbContext>().UseSqlite(connection).Options;
            await using (var setupContext = new SourceDbContext(dbContextOptions))
                await setupContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            IBusControl bus = global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(_ => { });
            ServiceProvider provider = new ServiceCollection()
                .AddSingleton<IBus>(bus)
                .AddSingleton(bus)
                .AddScoped(_ => new SourceDbContext(dbContextOptions))
                .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            await bus.StartAsync(TestContext.Current.CancellationToken);
            return new SourceEnvironment(bus, connection, dbContextOptions, provider);
        }

        public EntityFrameworkTransactionalOutboxSource<IBus, SourceDbContext> CreateSource(
            OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, SourceDbContext>>? options = null) =>
            EntityFrameworkTransactionalOutboxSourceTests.CreateSource(_provider, deliveryOptions: options);

        public async Task SeedPendingOutboxAsync(int messageCount)
        {
            await using var dbContext = new SourceDbContext(_dbContextOptions);
            var state = new OutboxState
            {
                OutboxId = Guid.NewGuid(),
                BusKey = "source",
                Created = Now.UtcDateTime,
                Status = OutboxDeliveryStatus.Pending,
            };
            dbContext.Add(state);
            for (var sequence = 0; sequence < messageCount; sequence++)
                dbContext.Add(CreateMessage(state.OutboxId, sequence));

            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public async Task<SourceSnapshot> ReadSnapshotAsync()
        {
            await using var dbContext = new SourceDbContext(_dbContextOptions);
            OutboxState? state = await dbContext.Set<OutboxState>()
                .AsNoTracking()
                .SingleOrDefaultAsync(TestContext.Current.CancellationToken);
            int messageCount = await dbContext.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken);
            return new SourceSnapshot(state, messageCount);
        }

        public async ValueTask DisposeAsync()
        {
            await _bus.StopAsync(CancellationToken.None);
            await _provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
