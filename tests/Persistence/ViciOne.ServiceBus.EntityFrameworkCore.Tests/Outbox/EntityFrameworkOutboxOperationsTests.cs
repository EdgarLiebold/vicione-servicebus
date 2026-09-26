using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkOutboxOperationsTests
{
    private static readonly DateTimeOffset Created = new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);
    private const string FirstBusKey = "first-bus-v1";
    private const string SecondBusKey = "second-bus-v1";

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "constructor-rejects-missing-dependencies")]
    public async Task Constructor_RejectsEveryMissingDependencyAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        var notification = new RecordingNotification();
        BusPersistenceIdentity<IFirstBus> identity = BusPersistenceIdentity<IFirstBus>.Create(FirstBusKey);

        Assert.Throws<ArgumentNullException>(() =>
            new EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>(null!, notification, identity));
        Assert.Throws<ArgumentNullException>(() =>
            new EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>(fixture.DbContext, null!, identity));
        Assert.Throws<ArgumentNullException>(() =>
            new EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>(fixture.DbContext, notification, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "mutation-identifiers-must-be-nonempty")]
    public async Task Mutations_RejectAnEmptyOutboxIdentifierBeforeDatabaseAccessAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        var operations = CreateOperations(fixture.DbContext, new RecordingNotification());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            operations.RequeueAsync(Guid.Empty, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            operations.DiscardAsync(Guid.Empty, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "default-and-maximum-page-sizes-are-accepted")]
    public async Task GetQuarantined_DeclaresTheDefaultAndAcceptsTheExactMaximumPageSizeAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        var operations = CreateOperations(fixture.DbContext, new RecordingNotification());

        object? defaultLimit = typeof(IEntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>)
            .GetMethod(nameof(IEntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>.GetQuarantinedAsync))!
            .GetParameters()
            .Single(parameter => parameter.Name == "limit")
            .DefaultValue;

        Assert.Equal(100, defaultLimit);
        Assert.Empty(await operations.GetQuarantinedAsync(
            EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>.MaximumQuarantinePageSize,
            TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "every-database-operation-observes-caller-cancellation")]
    public async Task Operations_ObservePreCanceledCallerTokensAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        var operations = CreateOperations(fixture.DbContext, new RecordingNotification());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            operations.GetQuarantinedAsync(1, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            operations.RequeueAsync(Guid.NewGuid(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            operations.DiscardAsync(Guid.NewGuid(), cancellation.Token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "quarantine-list-is-bounded-owned-and-deterministic")]
    public async Task GetQuarantined_ReturnsOnlyTheOwnedBusInDeterministicBoundedOrderAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        Guid laterId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        Guid earlierId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid oldestId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        fixture.DbContext.AddRange(
            CreateState(laterId, FirstBusKey, OutboxDeliveryStatus.Quarantined),
            CreateState(earlierId, FirstBusKey, OutboxDeliveryStatus.Quarantined),
            CreateState(oldestId, FirstBusKey, OutboxDeliveryStatus.Quarantined, Created.AddMinutes(-1)),
            CreateState(Guid.NewGuid(), FirstBusKey, OutboxDeliveryStatus.Pending),
            CreateState(Guid.Parse("00000000-0000-0000-0000-000000000001"), SecondBusKey,
                OutboxDeliveryStatus.Quarantined));
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var notification = new RecordingNotification();
        var operations = CreateOperations(fixture.DbContext, notification);

        IReadOnlyList<OutboxQuarantineEntry> entries = await operations.GetQuarantinedAsync(2, TestContext.Current.CancellationToken);

        Assert.Equal([oldestId, earlierId], entries.Select(x => x.OutboxId));
        Assert.All(entries, x => Assert.Equal(OutboxFailureKind.Permanent, x.FailureKind));
        Assert.All(entries, x => Assert.Equal(OutboxFailureCode.TransportSendFailed, x.FailureCode));
        Assert.All(entries, x => Assert.Equal(typeof(InvalidOperationException).FullName, x.ExceptionType));
        Assert.All(entries, x => Assert.Equal(17, x.FailedSequenceNumber));
        Assert.All(entries, x => Assert.Equal(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), x.FailedMessageId));
        Assert.Equal(0, notification.DeliveredCount);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            operations.GetQuarantinedAsync(0, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            operations.GetQuarantinedAsync(EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext>.MaximumQuarantinePageSize + 1,
                TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("empty-outbox", "outboxId", typeof(ArgumentException))]
    [InlineData("zero-attempts", "deliveryAttempts", typeof(ArgumentOutOfRangeException))]
    [InlineData("negative-attempts", "deliveryAttempts", typeof(ArgumentOutOfRangeException))]
    [InlineData("missing-kind", "failureKind", typeof(ArgumentOutOfRangeException))]
    [InlineData("unknown-kind", "failureKind", typeof(ArgumentOutOfRangeException))]
    [InlineData("missing-code", "failureCode", typeof(ArgumentOutOfRangeException))]
    [InlineData("unknown-code", "failureCode", typeof(ArgumentOutOfRangeException))]
    [InlineData("missing-time", "failureTime", typeof(ArgumentNullException))]
    [InlineData("missing-sequence", "failedSequenceNumber", typeof(ArgumentOutOfRangeException))]
    [InlineData("zero-sequence", "failedSequenceNumber", typeof(ArgumentOutOfRangeException))]
    [InlineData("negative-sequence", "failedSequenceNumber", typeof(ArgumentOutOfRangeException))]
    [InlineData("missing-message", "failedMessageId", typeof(ArgumentException))]
    [InlineData("empty-message", "failedMessageId", typeof(ArgumentException))]
    [InlineData("negative-counter-wrong-kind", "deliveryAttempts", typeof(ArgumentOutOfRangeException))]
    [InlineData("negative-counter-wrong-code", "deliveryAttempts", typeof(ArgumentOutOfRangeException))]
    [InlineData("zero-invariant-counter", "deliveryAttempts", typeof(ArgumentOutOfRangeException))]
    [InlineData("empty-message-wrong-kind", "failedMessageId", typeof(ArgumentException))]
    [InlineData("empty-message-wrong-code", "failedMessageId", typeof(ArgumentException))]
    [InlineData("missing-invariant-message", "failedMessageId", typeof(ArgumentException))]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "corrupt-quarantine-evidence-fails-closed-without-mutation")]
    public async Task GetQuarantined_RejectsCorruptPersistedEvidenceWithoutMutatingItAsync(
        string corruption, string expectedParameter, Type expectedException)
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        OutboxState state = CreateState(Guid.NewGuid(), FirstBusKey, OutboxDeliveryStatus.Quarantined);
        if (corruption is "negative-counter-wrong-kind" or "zero-invariant-counter" or "empty-message-wrong-kind" or "missing-invariant-message")
            state.LastFailureCode = OutboxFailureCode.InvalidDeliveryAttemptCount;
        if (corruption is "negative-counter-wrong-code" or "zero-invariant-counter" or "empty-message-wrong-code" or "missing-invariant-message")
            state.LastFailureKind = OutboxFailureKind.InvariantViolation;
        fixture.DbContext.Add(state);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        (string Column, object Value) change = corruption switch
        {
            "empty-outbox" => ("OutboxId", Guid.Empty),
            "zero-attempts" => ("DeliveryAttempts", 0),
            "negative-attempts" => ("DeliveryAttempts", -1),
            "missing-kind" => ("LastFailureKind", 0),
            "unknown-kind" => ("LastFailureKind", int.MaxValue),
            "missing-code" => ("LastFailureCode", 0),
            "unknown-code" => ("LastFailureCode", int.MaxValue),
            "missing-time" => ("LastFailureTime", DBNull.Value),
            "missing-sequence" => ("FailedSequenceNumber", DBNull.Value),
            "zero-sequence" => ("FailedSequenceNumber", 0),
            "negative-sequence" => ("FailedSequenceNumber", -1),
            "missing-message" or "missing-invariant-message" => ("FailedMessageId", DBNull.Value),
            "empty-message" => ("FailedMessageId", Guid.Empty),
            "negative-counter-wrong-kind" or "negative-counter-wrong-code" => ("DeliveryAttempts", -1),
            "zero-invariant-counter" => ("DeliveryAttempts", 0),
            "empty-message-wrong-kind" or "empty-message-wrong-code" => ("FailedMessageId", Guid.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(corruption)),
        };
        await using var command = fixture.DbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"UPDATE \"OutboxState\" SET \"{change.Column}\" = @value";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@value";
        parameter.Value = change.Value;
        command.Parameters.Add(parameter);
        int changed = await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, changed);
        fixture.DbContext.ChangeTracker.Clear();
        string before = System.Text.Json.JsonSerializer.Serialize(await fixture.DbContext.Set<OutboxState>()
            .AsNoTracking().SingleAsync(TestContext.Current.CancellationToken));
        var notification = new RecordingNotification();
        var operations = CreateOperations(fixture.DbContext, notification);

        ArgumentException exception = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            operations.GetQuarantinedAsync(1, TestContext.Current.CancellationToken));

        Assert.IsType(expectedException, exception);
        Assert.Equal(expectedParameter, exception.ParamName);
        fixture.DbContext.ChangeTracker.Clear();
        OutboxState persisted = await fixture.DbContext.Set<OutboxState>()
            .AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(persisted));
        Assert.Equal(OutboxDeliveryStatus.Quarantined, persisted.Status);
        Assert.Equal(0, notification.DeliveredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "requeue-resets-owned-quarantine-and-signals")]
    public async Task Requeue_ResetsEveryFailureFieldAndSignalsOnlyAfterPersistenceAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        OutboxState state = CreateState(Guid.NewGuid(), FirstBusKey,
            OutboxDeliveryStatus.Quarantined);
        fixture.DbContext.Add(state);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var notification = new RecordingNotification();
        var operations = CreateOperations(fixture.DbContext, notification);

        await operations.RequeueAsync(state.OutboxId, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(OutboxDeliveryStatus.Pending, persisted.Status);
        Assert.Equal(0, persisted.DeliveryAttempts);
        Assert.Equal(OutboxFailureKind.None, persisted.LastFailureKind);
        Assert.Equal(OutboxFailureCode.None, persisted.LastFailureCode);
        Assert.Null(persisted.NextDeliveryTime);
        Assert.Null(persisted.LastFailureTime);
        Assert.Null(persisted.LastExceptionType);
        Assert.Null(persisted.FailedSequenceNumber);
        Assert.Null(persisted.FailedMessageId);
        Assert.Null(persisted.Delivered);
        Assert.Equal(1, notification.DeliveredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "discard-removes-owned-quarantine-and-messages")]
    public async Task Discard_RemovesTheOwnedQuarantineAndMessagesWithoutTouchingAnotherBusAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        OutboxState owned = CreateState(Guid.NewGuid(), FirstBusKey,
            OutboxDeliveryStatus.Quarantined);
        OutboxState foreign = CreateState(Guid.NewGuid(), SecondBusKey,
            OutboxDeliveryStatus.Quarantined);
        fixture.DbContext.AddRange(owned, foreign, CreateMessage(1, owned.OutboxId), CreateMessage(2, foreign.OutboxId));
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var operations = CreateOperations(fixture.DbContext, new RecordingNotification());

        await operations.DiscardAsync(owned.OutboxId, TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        OutboxState remainingState = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        OutboxMessage remainingMessage = await fixture.DbContext.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(foreign.OutboxId, remainingState.OutboxId);
        Assert.Equal(foreign.OutboxId, remainingMessage.OutboxId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "discard-rolls-back-when-claimed-state-disappears")]
    public async Task Discard_FailsClosedAndRollsBackWhenTheClaimedStateDisappearsAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        OutboxState state = CreateState(Guid.NewGuid(), FirstBusKey,
            OutboxDeliveryStatus.Quarantined);
        fixture.DbContext.Add(state);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        await fixture.DbContext.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER "delete_claimed_outbox_state"
            AFTER UPDATE OF "Status" ON "OutboxState"
            WHEN NEW."Status" = 5
            BEGIN
                DELETE FROM "OutboxState" WHERE "OutboxId" = NEW."OutboxId";
            END;
            """, TestContext.Current.CancellationToken);
        var operations = CreateOperations(fixture.DbContext, new RecordingNotification());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            operations.DiscardAsync(state.OutboxId, TestContext.Current.CancellationToken));

        Assert.Contains(state.OutboxId.ToString(), exception.Message, StringComparison.Ordinal);
        fixture.DbContext.ChangeTracker.Clear();
        OutboxState persisted = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(OutboxDeliveryStatus.Quarantined, persisted.Status);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "foreign-and-nonquarantined-state-fail-closed")]
    public async Task Mutations_RejectForeignAndNonQuarantinedStatesWithoutChangingEitherAsync()
    {
        await using OperationsFixture fixture = await OperationsFixture.CreateAsync();
        OutboxState pending = CreateState(Guid.NewGuid(), FirstBusKey,
            OutboxDeliveryStatus.Pending);
        OutboxState foreign = CreateState(Guid.NewGuid(), SecondBusKey,
            OutboxDeliveryStatus.Quarantined);
        fixture.DbContext.AddRange(pending, foreign);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var operations = CreateOperations(fixture.DbContext, new RecordingNotification());

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

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OPERATIONS", "concurrent-operator-decisions-have-one-winner")]
    public async Task ConcurrentMutations_ApplyExactlyOneOperatorDecisionAsync()
    {
        const int contenderCount = 24;
        await using ConcurrentOperationsFixture fixture = await ConcurrentOperationsFixture.CreateAsync();
        Guid outboxId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        await using (OperationsDbContext setupContext = fixture.CreateContext())
        {
            setupContext.AddRange(
                CreateState(outboxId, FirstBusKey, OutboxDeliveryStatus.Quarantined),
                CreateMessage(1, outboxId));
            await setupContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var notification = new RecordingNotification();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool>[] contenders = Enumerable.Range(0, contenderCount)
            .Select(async index =>
            {
                await start.Task.WaitAsync(TestContext.Current.CancellationToken);
                await using OperationsDbContext dbContext = fixture.CreateContext();
                var operations = CreateOperations(dbContext, notification);

                try
                {
                    if (index % 2 == 0)
                        await operations.RequeueAsync(outboxId, TestContext.Current.CancellationToken);
                    else
                        await operations.DiscardAsync(outboxId, TestContext.Current.CancellationToken);

                    return true;
                }
                catch (Exception exception) when (exception is InvalidOperationException or KeyNotFoundException)
                {
                    return false;
                }
            })
            .ToArray();

        start.SetResult();
        bool[] results = await Task.WhenAll(contenders);

        Assert.Single(results, applied => applied);
        await using OperationsDbContext verificationContext = fixture.CreateContext();
        OutboxState? state = await verificationContext.Set<OutboxState>()
            .AsNoTracking()
            .SingleOrDefaultAsync(TestContext.Current.CancellationToken);
        OutboxMessage[] messages = await verificationContext.Set<OutboxMessage>()
            .AsNoTracking()
            .ToArrayAsync(TestContext.Current.CancellationToken);
        if (state is null)
        {
            Assert.Empty(messages);
            Assert.Equal(0, notification.DeliveredCount);
        }
        else
        {
            Assert.Equal(OutboxDeliveryStatus.Pending, state.Status);
            Assert.Single(messages);
            Assert.Equal(1, notification.DeliveredCount);
        }
    }

    private static OutboxState CreateState(Guid id, string busKey, OutboxDeliveryStatus status, DateTimeOffset? created = null)
    {
        DateTimeOffset createdAt = created ?? Created;

        return new OutboxState
        {
            OutboxId = id,
            BusKey = busKey,
            Created = createdAt,
            Status = status,
            NextDeliveryTime = createdAt.AddMinutes(1),
            DeliveryAttempts = 4,
            LastFailureKind = OutboxFailureKind.Permanent,
            LastFailureCode = OutboxFailureCode.TransportSendFailed,
            LastFailureTime = createdAt,
            LastExceptionType = typeof(InvalidOperationException).FullName,
            FailedSequenceNumber = 17,
            FailedMessageId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            Delivered = createdAt
        };
    }

    private static EntityFrameworkOutboxOperations<IFirstBus, OperationsDbContext> CreateOperations(
        OperationsDbContext dbContext,
        RecordingNotification notification) =>
        new(dbContext, notification, BusPersistenceIdentity<IFirstBus>.Create(FirstBusKey));

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
        public Task WaitForDeliveryAsync(CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public void SignalDelivery() => Interlocked.Increment(ref _deliveredCount);
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

        public static async Task<OperationsFixture> CreateAsync()
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

    private sealed class ConcurrentOperationsFixture : IAsyncDisposable
    {
        private readonly DbContextOptions<OperationsDbContext> _options;
        private readonly string _path;

        private ConcurrentOperationsFixture(string path, DbContextOptions<OperationsDbContext> options)
        {
            _path = path;
            _options = options;
        }

        public static async Task<ConcurrentOperationsFixture> CreateAsync()
        {
            string path = Path.Combine(Path.GetTempPath(), $"vicione-outbox-operations-{Guid.NewGuid():N}.db");
            DbContextOptions<OperationsDbContext> options = new DbContextOptionsBuilder<OperationsDbContext>()
                .UseSqlite($"Data Source={path};Default Timeout=30;Pooling=False")
                .Options;
            var fixture = new ConcurrentOperationsFixture(path, options);
            await using OperationsDbContext dbContext = fixture.CreateContext();
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            return fixture;
        }

        public OperationsDbContext CreateContext() => new(_options);

        public ValueTask DisposeAsync()
        {
            File.Delete(_path);
            File.Delete(_path + "-wal");
            File.Delete(_path + "-shm");
            return ValueTask.CompletedTask;
        }
    }
}
