namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Outbox;

using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class EntityFrameworkOutboxWriteCoordinatorTests
{
    private static readonly DateTimeOffset Now =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-WRITE", "concurrent-writes-share-one-state")]
    public async Task ConcurrentWrites_CreateOneStateAndRetainEveryDistinctMessage()
    {
        await using OutboxFixture fixture = await OutboxFixture.Create();
        using EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid[] messageIds = Enumerable.Range(0, 32).Select(_ => Guid.NewGuid()).ToArray();
        Task[] writes = messageIds.Select(async (messageId, index) =>
        {
            await start.Task;
            await context.AddSend(CreateSendContext(messageId, index));
        }).ToArray();

        start.SetResult();
        await Task.WhenAll(writes);

        OutboxState state = Assert.Single(fixture.DbContext.Set<OutboxState>().Local);
        OutboxMessage[] messages = fixture.DbContext.Set<OutboxMessage>().Local.ToArray();
        Assert.Equal(messageIds.Length, messages.Length);
        Assert.Equal(messageIds.Order(), messages.Select(message => message.MessageId).Order());
        Assert.All(messages, message => Assert.Equal(state.OutboxId, message.OutboxId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-BUS-OUTBOX-WRITE", "committed-batch-rolls-to-new-state")]
    public async Task CommittedBatch_StartsANewStateAndNotifiesExactlyOncePerBatch()
    {
        await using OutboxFixture fixture = await OutboxFixture.Create();
        EntityFrameworkScopedBusContext<IBus, OutboxDbContext> context = fixture.CreateBusContext();
        await context.AddSend(CreateSendContext(Guid.NewGuid(), 1));
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        Guid firstOutboxId = Assert.Single(fixture.DbContext.Set<OutboxState>().Local).OutboxId;

        await context.AddSend(CreateSendContext(Guid.NewGuid(), 2));
        OutboxState[] states = fixture.DbContext.Set<OutboxState>().Local.ToArray();

        Assert.Equal(2, states.Length);
        Assert.Single(states, state => state.OutboxId == firstOutboxId);
        Assert.Single(states, state => state.OutboxId != firstOutboxId);
        Assert.Equal(1, fixture.Notification.DeliveredCount);

        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.Dispose();
        Assert.Equal(2, fixture.Notification.DeliveredCount);
    }

    private static MessageSendContext<OutboxProbe> CreateSendContext(Guid messageId, int sequence) => new(
        new OutboxProbe(sequence))
    {
        MessageId = messageId,
        Serializer = SystemTextJsonMessageSerializer.Instance,
    };

    public sealed record OutboxProbe(int Sequence);

    public sealed class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
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

        public static async Task<OutboxFixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var dbContext = new OutboxDbContext(
                new DbContextOptionsBuilder<OutboxDbContext>()
                    .UseSqlite(connection)
                    .Options);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var notification = new RecordingNotification();
            var services = new ServiceCollection().BuildServiceProvider();
            IBus bus = global::ViciOne.ServiceBus.Bus.Factory.CreateUsingInMemory(_ => { });

            return new OutboxFixture(connection, dbContext, bus, notification, services);
        }

        public EntityFrameworkScopedBusContext<IBus, OutboxDbContext> CreateBusContext() => new(
            Bus,
            DbContext,
            Notification,
            DispatchProxy.Create<IClientFactory, ThrowingClientFactoryProxy>(),
            _services,
            new FakeTimeProvider(Now));

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

    public sealed class RecordingNotification : IBusOutboxNotification
    {
        private int _deliveredCount;

        public int DeliveredCount => Volatile.Read(ref _deliveredCount);

        public void Delivered() => Interlocked.Increment(ref _deliveredCount);

        public Task WaitForDelivery(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The outbox write tests must not wait for delivery.");
    }
}
