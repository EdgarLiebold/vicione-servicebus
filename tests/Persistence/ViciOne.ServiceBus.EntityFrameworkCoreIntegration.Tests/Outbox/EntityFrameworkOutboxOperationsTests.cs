namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Outbox;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class EntityFrameworkOutboxOperationsTests
{
    private static readonly DateTime Created = new(2042, 3, 4, 5, 6, 7, DateTimeKind.Utc);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "quarantine-list-is-bounded-owned-and-deterministic")]
    public async Task GetQuarantined_ReturnsOnlyTheOwnedBusInDeterministicBoundedOrder()
    {
        await using OperationsFixture fixture = await OperationsFixture.Create();
        Guid laterId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid earlierId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        fixture.DbContext.AddRange(
            CreateState(laterId, EntityFrameworkBusOutboxIdentity<IFirstBus>.BusKey, OutboxDeliveryStatus.Quarantined),
            CreateState(earlierId, EntityFrameworkBusOutboxIdentity<IFirstBus>.BusKey, OutboxDeliveryStatus.Quarantined),
            CreateState(Guid.NewGuid(), EntityFrameworkBusOutboxIdentity<IFirstBus>.BusKey, OutboxDeliveryStatus.Pending),
            CreateState(Guid.Parse("00000000-0000-0000-0000-000000000001"), EntityFrameworkBusOutboxIdentity<ISecondBus>.BusKey,
                OutboxDeliveryStatus.Quarantined));
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var notification = new RecordingNotification();
        var operations = new EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>(fixture.DbContext, notification);

        IReadOnlyList<OutboxQuarantineEntry> entries = await operations.GetQuarantinedAsync(2, TestContext.Current.CancellationToken);

        Assert.Equal([earlierId, laterId], entries.Select(x => x.OutboxId));
        Assert.All(entries, x => Assert.Equal(OutboxFailureKind.Permanent, x.FailureKind));
        Assert.Equal(0, notification.DeliveredCount);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            operations.GetQuarantinedAsync(0, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            operations.GetQuarantinedAsync(EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>.MaximumQuarantinePageSize + 1,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "requeue-resets-owned-quarantine-and-signals")]
    public async Task Requeue_ResetsEveryFailureFieldAndSignalsOnlyAfterPersistence()
    {
        await using OperationsFixture fixture = await OperationsFixture.Create();
        OutboxState state = CreateState(Guid.NewGuid(), EntityFrameworkBusOutboxIdentity<IFirstBus>.BusKey,
            OutboxDeliveryStatus.Quarantined);
        fixture.DbContext.Add(state);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var notification = new RecordingNotification();
        var operations = new EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>(fixture.DbContext, notification);

        await operations.RequeueAsync(state.OutboxId, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(OutboxDeliveryStatus.Pending, persisted.Status);
        Assert.Equal(0, persisted.DeliveryAttempts);
        Assert.Equal(OutboxFailureKind.None, persisted.LastFailureKind);
        Assert.Null(persisted.NextDeliveryTime);
        Assert.Null(persisted.LastFailureTime);
        Assert.Null(persisted.LastFailure);
        Assert.Null(persisted.FailedSequenceNumber);
        Assert.Null(persisted.FailedMessageId);
        Assert.Null(persisted.Delivered);
        Assert.Equal(1, notification.DeliveredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "discard-removes-owned-quarantine-and-messages")]
    public async Task Discard_RemovesTheOwnedQuarantineAndMessagesWithoutTouchingAnotherBus()
    {
        await using OperationsFixture fixture = await OperationsFixture.Create();
        OutboxState owned = CreateState(Guid.NewGuid(), EntityFrameworkBusOutboxIdentity<IFirstBus>.BusKey,
            OutboxDeliveryStatus.Quarantined);
        OutboxState foreign = CreateState(Guid.NewGuid(), EntityFrameworkBusOutboxIdentity<ISecondBus>.BusKey,
            OutboxDeliveryStatus.Quarantined);
        fixture.DbContext.AddRange(owned, foreign, CreateMessage(1, owned.OutboxId), CreateMessage(2, foreign.OutboxId));
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var operations = new EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>(fixture.DbContext, new RecordingNotification());

        await operations.DiscardAsync(owned.OutboxId, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState remainingState = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        OutboxMessage remainingMessage = await fixture.DbContext.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(foreign.OutboxId, remainingState.OutboxId);
        Assert.Equal(foreign.OutboxId, remainingMessage.OutboxId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "foreign-and-nonquarantined-state-fail-closed")]
    public async Task Mutations_RejectForeignAndNonQuarantinedStatesWithoutChangingEither()
    {
        await using OperationsFixture fixture = await OperationsFixture.Create();
        OutboxState pending = CreateState(Guid.NewGuid(), EntityFrameworkBusOutboxIdentity<IFirstBus>.BusKey,
            OutboxDeliveryStatus.Pending);
        OutboxState foreign = CreateState(Guid.NewGuid(), EntityFrameworkBusOutboxIdentity<ISecondBus>.BusKey,
            OutboxDeliveryStatus.Quarantined);
        fixture.DbContext.AddRange(pending, foreign);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var operations = new EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>(fixture.DbContext, new RecordingNotification());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            operations.RequeueAsync(pending.OutboxId, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            operations.DiscardAsync(foreign.OutboxId, TestContext.Current.CancellationToken));
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState[] states = await fixture.DbContext.Set<OutboxState>().OrderBy(x => x.OutboxId)
            .ToArrayAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, states.Length);
        Assert.Contains(states, x => x.OutboxId == pending.OutboxId && x.Status == OutboxDeliveryStatus.Pending);
        Assert.Contains(states, x => x.OutboxId == foreign.OutboxId && x.Status == OutboxDeliveryStatus.Quarantined);
    }

    private static OutboxState CreateState(Guid id, string busKey, OutboxDeliveryStatus status) => new()
    {
        OutboxId = id,
        BusKey = busKey,
        Created = Created,
        Status = status,
        NextDeliveryTime = Created.AddMinutes(1),
        DeliveryAttempts = 4,
        LastFailureKind = OutboxFailureKind.Permanent,
        LastFailureTime = Created,
        LastFailure = "failure",
        FailedSequenceNumber = 17,
        FailedMessageId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        Delivered = Created
    };

    private static OutboxMessage CreateMessage(long sequenceNumber, Guid outboxId) => new()
    {
        SequenceNumber = sequenceNumber,
        OutboxId = outboxId,
        MessageId = Guid.NewGuid(),
        SentTime = Created,
        ContentType = "application/vnd.vicione.servicebus+json",
        MessageType = "urn:message:test",
        Body = "{}"
    };

    private interface IFirstBus : IBus;
    private interface ISecondBus : IBus;

    private sealed class RecordingNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<IFirstBus, OperationsDbContext>>
    {
        private int _deliveredCount;
        public int DeliveredCount => Volatile.Read(ref _deliveredCount);
        public Task WaitForDelivery(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Delivered() => Interlocked.Increment(ref _deliveredCount);
    }

    private sealed class OperationsDbContext(DbContextOptions<OperationsDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    private sealed class OperationsFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private OperationsFixture(SqliteConnection connection, OperationsDbContext dbContext)
        {
            _connection = connection;
            DbContext = dbContext;
        }

        public OperationsDbContext DbContext { get; }

        public static async Task<OperationsFixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var context = new OperationsDbContext(new DbContextOptionsBuilder<OperationsDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            return new OperationsFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
