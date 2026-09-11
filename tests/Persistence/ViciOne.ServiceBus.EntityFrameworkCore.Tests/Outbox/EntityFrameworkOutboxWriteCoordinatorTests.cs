using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkOutboxWriteCoordinatorTests
{
    private static readonly DateTimeOffset Now =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "commit-persists-business-and-intent")]
    public async Task Commit_PersistsBusinessDataAndOutboxIntentThroughTheSameContextAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "committed");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        DurableSendRecord message = await fixture.DbContext.Set<DurableSendRecord>()
            .SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("default", message.StoreKey);
        Assert.Equal(DurableSendStatus.Pending, message.Status);
        Assert.Equal("vicione.tests.transactional-outbox;v=1", message.ContractIdentity);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "commit-without-message-persists-business")]
    public async Task Commit_WithoutAStagedMessageStillPersistsBusinessChangesAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "business-only");
        fixture.DbContext.Add(business);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<DurableSendRecord>().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-DURABLE-CAPACITY", "transactional-outbox-counts-payload-and-metadata")]
    public async Task StagedMessage_AccountsForPayloadAndMetadataAgainstTheCapacityLimitAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        MessageSendContext<OutboxProbe> sendContext = CreateSendContext(Guid.NewGuid(), 1);
        sendContext.Headers.Set("x-capacity-probe", new string('x', 128));

        await context.AddSendAsync(sendContext, TestContext.Current.CancellationToken);

        DurableSendRecord message = Assert.Single(fixture.DbContext.Set<DurableSendRecord>().Local);
        DurableSendCapacityState capacity = Assert.Single(fixture.DbContext.Set<DurableSendCapacityState>().Local);
        byte[] metadata = Assert.IsType<byte[]>(message.Metadata);
        long expectedStorageSize = checked(message.Body.LongLength + metadata.LongLength);
        Assert.NotEmpty(metadata);
        Assert.Equal(expectedStorageSize, message.StorageSize);
        Assert.Equal((1, expectedStorageSize), (capacity.StoredCount, capacity.StoredBytes));

        await context.AbortAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "abort-detaches-only-session-intent")]
    public async Task Abort_DetachesOnlyTheSessionOutboxAndRetainsBusinessChangesAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "retained");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        await context.AbortAsync(TestContext.Current.CancellationToken);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<DurableSendRecord>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, Assert.Single(await fixture.DbContext.Set<DurableSendCapacityState>()
            .ToListAsync(TestContext.Current.CancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "dispose-without-commit-fails-and-detaches")]
    public async Task Dispose_WithoutCommitFailsLoudlyAfterDetachingOnlyTheOutboxAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var business = new BusinessRecord(Guid.NewGuid(), "survives-disposal");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(context.Dispose);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Contains("disposed without commit", failure.Message, StringComparison.Ordinal);
        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<DurableSendRecord>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, Assert.Single(await fixture.DbContext.Set<DurableSendCapacityState>()
            .ToListAsync(TestContext.Current.CancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "externally-saved-session-is-signaled-once")]
    public async Task Commit_AfterExternalSaveRecognizesThePersistedSessionAndSignalsExactlyOnceAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        await context.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Single(await fixture.DbContext.Set<DurableSendRecord>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, Assert.Single(await fixture.DbContext.Set<DurableSendCapacityState>()
            .ToListAsync(TestContext.Current.CancellationToken)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "externally-saved-session-survives-context-disposal-order")]
    public async Task Dispose_AfterExternalSaveDoesNotAccessAnAlreadyDisposedDbContextAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await fixture.DbContext.DisposeAsync();

        Exception? failure = Record.Exception(context.Dispose);

        Assert.Null(failure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-WRITE", "concurrent-writes-share-one-state")]
    public async Task ConcurrentWrites_CreateOneStateAndRetainEveryDistinctMessageAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid[] messageIds = Enumerable.Range(0, 32).Select(_ => Guid.NewGuid()).ToArray();
        Task[] writes = messageIds.Select(async (messageId, index) =>
        {
            await start.Task;
            await context.AddSendAsync(CreateSendContext(messageId, index));
        }).ToArray();

        start.SetResult();
        await Task.WhenAll(writes);

        DurableSendRecord[] messages = fixture.DbContext.Set<DurableSendRecord>().Local.ToArray();
        Assert.Equal(messageIds.Length, messages.Length);
        Assert.Equal(messageIds.Order(), messages.Select(message => message.Id).Order());
        Assert.All(messages, message => Assert.Equal("default", message.StoreKey));

        await context.AbortAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-WRITE", "committed-batch-rolls-to-new-state")]
    public async Task CommittedBatch_StartsANewStateAndNotifiesExactlyOncePerBatchAsync()
    {
        await using OutboxFixture fixture = await OutboxFixture.CreateAsync();
        EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        await context.CommitAsync(TestContext.Current.CancellationToken);
        Guid firstOutboxId = Assert.Single(fixture.DbContext.Set<DurableSendRecord>().Local).Id;

        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 2), TestContext.Current.CancellationToken);
        DurableSendRecord[] states = fixture.DbContext.Set<DurableSendRecord>().Local.ToArray();

        Assert.Equal(2, states.Length);
        Assert.Single(states, state => state.Id == firstOutboxId);
        Assert.Single(states, state => state.Id != firstOutboxId);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        context.Dispose();
        Assert.Equal(2, (await fixture.DbContext.Set<DurableSendRecord>()
            .ToListAsync(TestContext.Current.CancellationToken)).Count);
    }

    private static MessageSendContext<OutboxProbe> CreateSendContext(Guid messageId, int sequence) => new(
        new OutboxProbe(sequence))
    {
        MessageId = messageId,
        Serializer = ServiceBusMetadataJson.MessageSerializer,
        DestinationAddress = new Uri("loopback://transactional-outbox/probe"),
    };

    public sealed record OutboxProbe(int Sequence);

    public sealed record BusinessRecord(Guid Id, string Value);

    public sealed class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : DbContext(options)
    {
        public DbSet<BusinessRecord> BusinessRecords => Set<BusinessRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddViciOneReliableMessaging();
            modelBuilder.Entity<BusinessRecord>().HasKey(x => x.Id);
        }
    }

    private sealed class OutboxFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _services;

        private OutboxFixture(
            SqliteConnection connection,
            OutboxDbContext dbContext,
            IBus bus,
            RecordingNotification notification,
            ServiceProvider services)
        {
            _connection = connection;
            DbContext = dbContext;
            Bus = bus;
            Notification = notification;
            _services = services;
        }

        public IBus Bus { get; }

        public OutboxDbContext DbContext { get; }

        public RecordingNotification Notification { get; }

        public static async Task<OutboxFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var dbContext = new OutboxDbContext(
                new DbContextOptionsBuilder<OutboxDbContext>()
                    .UseSqlite(connection)
                    .Options);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var notification = new RecordingNotification();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddViciOneMessageContracts(catalog =>
                catalog.Register<OutboxProbe>("vicione.tests.transactional-outbox"));
            serviceCollection.AddViciOneReliableMessaging<IBus>(options =>
            {
                options.MaximumStoredCount = 100;
                options.MaximumStoredBytes = 1024 * 1024;
            });
            ServiceProvider services = serviceCollection.BuildServiceProvider();
            IBus bus = global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(_ => { });

            return new OutboxFixture(connection, dbContext, bus, notification, services);
        }

        public EntityFrameworkScopedBusContext<IBus, OutboxDbContext> CreateBusContext() => new(
            Bus,
            DbContext,
            Notification,
            DispatchProxy.Create<IClientFactory, ThrowingClientFactoryProxy>(),
            _services,
            new FakeTimeProvider(Now),
            BusPersistenceIdentity<IBus>.Create("default"));

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
            await _services.DisposeAsync();
        }
    }

    public class ThrowingClientFactoryProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The outbox write tests must not resolve a request client.");
    }

    public sealed class RecordingNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, OutboxDbContext>>
    {
        private int _deliveredCount;

        public int DeliveredCount => Volatile.Read(ref _deliveredCount);

        public void SignalDelivery() => Interlocked.Increment(ref _deliveredCount);

        public Task WaitForDeliveryAsync(CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); throw new InvalidOperationException("The outbox write tests must not wait for delivery."); }
    }
}
