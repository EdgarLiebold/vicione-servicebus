using System.Collections.Concurrent;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Outbox;

public sealed class ConsumerOutboxRecoveryTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    [InlineData(0, 2)]
    [InlineData(2, 2)]
    [InlineData(4, 2)]
    [InlineData(0, 10)]
    [InlineData(2, 10)]
    [InlineData(4, 10)]
    [RequirementCoverage("REQ-VSB-EF-INBOX-DEDUPLICATION", "corrupt-consumer-outbox-retained-and-repaired-across-windows")]
    public Task CorruptPersistedMessage_PreservesRowsAndRecoversAfterRepairAsync(int corruptIndex, int deliveryLimit) =>
        VerifyRecoveryAsync(corruptIndex, deliveryLimit, corruptDestination: true);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    [InlineData(0, 2)]
    [InlineData(2, 2)]
    [InlineData(4, 2)]
    [InlineData(0, 10)]
    [InlineData(2, 10)]
    [InlineData(4, 10)]
    [RequirementCoverage("REQ-VSB-EF-INBOX-DEDUPLICATION", "send-failure-preserves-committed-window-and-replays-only-uncommitted-effects")]
    public Task DeliveryFailure_PreservesCommittedWatermarkAcrossWindowsAsync(int failedIndex, int deliveryLimit) =>
        VerifyRecoveryAsync(failedIndex, deliveryLimit, corruptDestination: false);

    [Theory]
    [InlineData("save", 1)]
    [InlineData("save", 2)]
    [InlineData("save", 10)]
    [InlineData("commit", 1)]
    [InlineData("commit", 2)]
    [InlineData("commit", 10)]
    [RequirementCoverage("REQ-VSB-EF-INBOX-DEDUPLICATION", "delivery-save-and-commit-failure-preserve-recoverable-intent")]
    public Task PersistenceFailure_PreservesConsumedFenceAndRecoverableEffectsAsync(string phase, int deliveryLimit) =>
        VerifyRecoveryAsync(Math.Min(deliveryLimit * 2, 5) - 1, deliveryLimit, corruptDestination: false, persistenceFailure: phase);

    [Theory]
    [InlineData("late-failure", 1)]
    [InlineData("late-failure", 2)]
    [InlineData("late-failure", 10)]
    [InlineData("deadline", 1)]
    [InlineData("deadline", 2)]
    [InlineData("deadline", 10)]
    [RequirementCoverage("REQ-VSB-EF-INBOX-DEDUPLICATION", "pending-send-deadline-and-late-failure-retain-intent-for-recovery")]
    public Task PendingDelivery_PreservesIntentUntilFailureAndRecoversAsync(string mode, int deliveryLimit) =>
        VerifyRecoveryAsync(2, deliveryLimit, corruptDestination: false, pendingSend: mode);

    private static async Task VerifyRecoveryAsync(int corruptIndex, int deliveryLimit, bool corruptDestination,
        string? persistenceFailure = null, string? pendingSend = null)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using RecoveryFixture fixture = await RecoveryFixture.CreateAsync(deliveryLimit, pendingSend == "deadline");
        fixture.Probe.PendingSendMode = pendingSend;
        await fixture.SubmitAsync(token);
        Guid consumerId = await fixture.Probe.ConsumptionCommitted.Task.WaitAsync(fixture.Timeout, token);
        Guid otherConsumer = Guid.NewGuid();
        Guid otherInput = Guid.NewGuid();
        OutboxMessage[] original;
        string[] retained;
        string[] neighborRows;
        string[] neighborInbox;
        Uri originalDestination;
        await using (RecoveryDbContext db = fixture.OpenDb())
        {
            original = await db.Set<OutboxMessage>().OrderBy(x => x.SequenceNumber).ToArrayAsync(token);
            Assert.Equal(5, original.Length);
            Assert.All(original, row => Assert.Equal(fixture.Probe.InputId, row.InboxMessageId));
            Assert.All(original, row => Assert.Equal(consumerId, row.InboxConsumerId));
            originalDestination = Assert.IsType<Uri>(original[corruptIndex].DestinationAddress);
            if (corruptDestination)
                original[corruptIndex].DestinationAddress = null;
            else if (persistenceFailure is null)
                fixture.Probe.FailSendId = fixture.Probe.EffectIds[corruptIndex];
            else
            {
                fixture.Probe.PersistencePhase = persistenceFailure;
                fixture.Probe.FailAfterSequence = original[corruptIndex].SequenceNumber;
            }
            db.AddRange(
                NeighborInbox(fixture.Probe.InputId, otherConsumer),
                NeighborInbox(otherInput, consumerId));
            db.AddRange(
                NeighborMessage(original[0], fixture.Probe.InputId, otherConsumer),
                NeighborMessage(original[0], otherInput, consumerId));
            await db.SaveChangesAsync(token);
            retained = original.Select(Snapshot).ToArray();
            neighborRows = await ReadNeighborRowsAsync(db, fixture.Probe.InputId, consumerId, token);
            neighborInbox = await ReadNeighborInboxAsync(db, fixture.Probe.InputId, consumerId, token);
        }

        fixture.Probe.ReleaseConsumption();
        int committedCount = corruptIndex / deliveryLimit * deliveryLimit;
        if (pendingSend is not null)
        {
            CancellationToken sendToken = await fixture.Probe.SendEntered.Task.WaitAsync(fixture.Timeout, token);
            Assert.True(sendToken.CanBeCanceled);
            Assert.False(fixture.Probe.ReceiveFailure.Task.IsCompleted);
            Assert.False(fixture.Probe.DrainCommitted.Task.IsCompleted);
            await using RecoveryDbContext pendingDb = fixture.OpenDb();
            InboxState pendingInbox = await pendingDb.Set<InboxState>().SingleAsync(
                x => x.MessageId == fixture.Probe.InputId && x.ConsumerId == consumerId, token);
            Assert.NotNull(pendingInbox.Consumed);
            Assert.Null(pendingInbox.Delivered);
            Assert.Equal(committedCount == 0 ? (long?)null : original[committedCount - 1].SequenceNumber,
                pendingInbox.LastSequenceNumber);
            Assert.Equal(retained, (await pendingDb.Set<OutboxMessage>()
                .Where(x => x.InboxMessageId == fixture.Probe.InputId && x.InboxConsumerId == consumerId)
                .OrderBy(x => x.SequenceNumber).ToArrayAsync(token)).Select(Snapshot));
            Assert.DoesNotContain(fixture.Probe.Deliveries, x => x.Message.Index >= corruptIndex);
            Assert.False(sendToken.IsCancellationRequested);
            Assert.False(fixture.Probe.ReceiveFailure.Task.IsCompleted);
            Assert.False(fixture.Probe.DrainCommitted.Task.IsCompleted);
            if (pendingSend == "late-failure")
                fixture.Probe.ReleaseSend();
        }
        Exception failure = await fixture.Probe.ReceiveFailure.Task.WaitAsync(fixture.Timeout, token);
        if (corruptDestination)
        {
            InvalidOperationException rejected = Assert.IsType<InvalidOperationException>(failure);
            Assert.Contains("DestinationAddress", rejected.Message, StringComparison.Ordinal);
        }
        else if (pendingSend == "deadline")
        {
            OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(failure);
            Assert.Equal(await fixture.Probe.SendEntered.Task, canceled.CancellationToken);
            Assert.True(canceled.CancellationToken.IsCancellationRequested);
            Assert.False(token.IsCancellationRequested);
            Assert.Equal(1, fixture.Probe.SendFailures);
        }
        else if (persistenceFailure is null)
        {
            Assert.Same(fixture.Probe.SendFailure, failure);
            Assert.Equal(1, fixture.Probe.SendFailures);
        }
        else
        {
            Assert.Same(fixture.Probe.PersistenceFailure, failure);
            Assert.Equal(1, fixture.Probe.PersistenceFailures);
        }
        Assert.Equal(1, fixture.Probe.ConsumerCalls);
        await using (RecoveryDbContext db = fixture.OpenDb())
        {
            InboxState inbox = await db.Set<InboxState>().SingleAsync(
                x => x.MessageId == fixture.Probe.InputId && x.ConsumerId == consumerId, token);
            Assert.NotNull(inbox.Consumed);
            Assert.Null(inbox.Delivered);
            Assert.Equal(committedCount == 0 ? (long?)null : original[committedCount - 1].SequenceNumber,
                inbox.LastSequenceNumber);
            OutboxMessage[] remaining = await db.Set<OutboxMessage>()
                .Where(x => x.InboxMessageId == fixture.Probe.InputId && x.InboxConsumerId == consumerId)
                .OrderBy(x => x.SequenceNumber).ToArrayAsync(token);
            Assert.Equal(retained, remaining.Select(Snapshot));
            Assert.Equal(neighborRows, await ReadNeighborRowsAsync(db, fixture.Probe.InputId, consumerId, token));
            Assert.Equal(neighborInbox, await ReadNeighborInboxAsync(db, fixture.Probe.InputId, consumerId, token));
            if (corruptDestination)
                remaining[corruptIndex].DestinationAddress = originalDestination;
            await db.SaveChangesAsync(token);
        }

        await fixture.SubmitAsync(token);
        await fixture.Probe.DrainCommitted.Task.WaitAsync(fixture.Timeout, token);
        int sentBeforeFailure = corruptIndex + (persistenceFailure is null ? 0 : 1);
        int replayedCount = sentBeforeFailure - committedCount;
        await fixture.Probe.WaitForEffectsAsync(5 + replayedCount, fixture.Timeout, token);
        await fixture.StopAsync();
        Assert.Equal(1, fixture.Probe.ConsumerCalls);
        Delivery[] delivered = fixture.Probe.Deliveries.ToArray();
        Assert.Equal(5 + replayedCount, delivered.Length);
        for (int index = 0; index < 5; index++)
        {
            Delivery[] matching = delivered.Where(x => x.Message.Index == index).ToArray();
            Assert.Equal(index >= committedCount && index < sentBeforeFailure ? 2 : 1, matching.Length);
            Assert.All(matching, item =>
            {
                Assert.Equal(fixture.Probe.InputId, item.Message.InputId);
                Assert.Equal(fixture.Probe.EffectIds[index], item.MessageId);
                Assert.Equal($"effect-{index}", item.Header);
            });
        }
        await using (RecoveryDbContext db = fixture.OpenDb())
        {
            InboxState inbox = await db.Set<InboxState>().SingleAsync(
                x => x.MessageId == fixture.Probe.InputId && x.ConsumerId == consumerId, token);
            Assert.NotNull(inbox.Delivered);
            Assert.Equal(original[^1].SequenceNumber, inbox.LastSequenceNumber);
            Assert.Empty(await db.Set<OutboxMessage>()
                .Where(x => x.InboxMessageId == fixture.Probe.InputId && x.InboxConsumerId == consumerId).ToArrayAsync(token));
            Assert.Equal(neighborRows, await ReadNeighborRowsAsync(db, fixture.Probe.InputId, consumerId, token));
            Assert.Equal(neighborInbox, await ReadNeighborInboxAsync(db, fixture.Probe.InputId, consumerId, token));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [RequirementCoverage("REQ-VSB-EF-INBOX-DEDUPLICATION", "cleanup-retry-retains-delivered-fence-and-neighbors-without-resending")]
    public async Task CleanupFailure_RetryRemovesOnlyOwnedRowsWithoutSendingAgainAsync(int deliveryLimit)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using RecoveryFixture fixture = await RecoveryFixture.CreateAsync(deliveryLimit);
        fixture.Probe.FailCleanup = true;
        await fixture.SubmitAsync(token);
        Guid consumerId = await fixture.Probe.ConsumptionCommitted.Task.WaitAsync(fixture.Timeout, token);
        string[] ownedRows;
        string[] neighbors;
        string[] neighborInbox;
        long lastSequence;
        await using (RecoveryDbContext db = fixture.OpenDb())
        {
            OutboxMessage[] rows = await db.Set<OutboxMessage>().OrderBy(x => x.SequenceNumber).ToArrayAsync(token);
            Assert.Equal(5, rows.Length);
            ownedRows = rows.Select(Snapshot).ToArray();
            lastSequence = rows[^1].SequenceNumber;
            Guid otherConsumer = Guid.NewGuid();
            Guid otherInput = Guid.NewGuid();
            db.AddRange(NeighborInbox(fixture.Probe.InputId, otherConsumer), NeighborInbox(otherInput, consumerId));
            db.AddRange(NeighborMessage(rows[0], fixture.Probe.InputId, otherConsumer), NeighborMessage(rows[0], otherInput, consumerId));
            await db.SaveChangesAsync(token);
            neighbors = await ReadNeighborRowsAsync(db, fixture.Probe.InputId, consumerId, token);
            neighborInbox = await ReadNeighborInboxAsync(db, fixture.Probe.InputId, consumerId, token);
        }
        fixture.Probe.ReleaseConsumption();
        Assert.Same(fixture.Probe.CleanupFailure,
            await fixture.Probe.ReceiveFailure.Task.WaitAsync(fixture.Timeout, token));
        Assert.Equal(1, fixture.Probe.CleanupFailures);
        await fixture.Probe.WaitForEffectsAsync(5, fixture.Timeout, token);
        await using (RecoveryDbContext db = fixture.OpenDb())
        {
            InboxState inbox = await db.Set<InboxState>().SingleAsync(
                x => x.MessageId == fixture.Probe.InputId && x.ConsumerId == consumerId, token);
            Assert.NotNull(inbox.Consumed);
            Assert.NotNull(inbox.Delivered);
            Assert.Equal(lastSequence, inbox.LastSequenceNumber);
            Assert.Equal(ownedRows, (await db.Set<OutboxMessage>()
                .Where(x => x.InboxMessageId == fixture.Probe.InputId && x.InboxConsumerId == consumerId)
                .OrderBy(x => x.SequenceNumber).ToArrayAsync(token)).Select(Snapshot));
            Assert.Equal(neighbors, await ReadNeighborRowsAsync(db, fixture.Probe.InputId, consumerId, token));
            Assert.Equal(neighborInbox, await ReadNeighborInboxAsync(db, fixture.Probe.InputId, consumerId, token));
        }
        await fixture.SubmitAsync(token);
        await fixture.Probe.DrainCommitted.Task.WaitAsync(fixture.Timeout, token);
        await fixture.StopAsync();
        Assert.Equal(1, fixture.Probe.ConsumerCalls);
        Assert.Equal(5, fixture.Probe.Deliveries.Count);
        Assert.Equal(fixture.Probe.EffectIds.Order(), fixture.Probe.Deliveries.Select(x => x.MessageId!.Value).Order());
        Assert.All(fixture.Probe.Deliveries, item =>
        {
            Assert.Equal(fixture.Probe.InputId, item.Message.InputId);
            Assert.Equal($"effect-{item.Message.Index}", item.Header);
        });
        await using (RecoveryDbContext db = fixture.OpenDb())
        {
            Assert.Empty(await db.Set<OutboxMessage>()
                .Where(x => x.InboxMessageId == fixture.Probe.InputId && x.InboxConsumerId == consumerId).ToArrayAsync(token));
            Assert.Equal(neighbors, await ReadNeighborRowsAsync(db, fixture.Probe.InputId, consumerId, token));
            Assert.Equal(neighborInbox, await ReadNeighborInboxAsync(db, fixture.Probe.InputId, consumerId, token));
        }
    }

    private static string Snapshot<T>(T value) => JsonSerializer.Serialize(value);

    private static async Task<string[]> ReadNeighborRowsAsync(RecoveryDbContext db, Guid input, Guid consumer, CancellationToken token) =>
        (await db.Set<OutboxMessage>().AsNoTracking()
            .Where(x => x.InboxMessageId != input || x.InboxConsumerId != consumer)
            .OrderBy(x => x.SequenceNumber).ToArrayAsync(token)).Select(Snapshot).ToArray();

    private static async Task<string[]> ReadNeighborInboxAsync(RecoveryDbContext db, Guid input, Guid consumer, CancellationToken token) =>
        (await db.Set<InboxState>().AsNoTracking()
            .Where(x => x.MessageId != input || x.ConsumerId != consumer)
            .OrderBy(x => x.Id).ToArrayAsync(token)).Select(Snapshot).ToArray();

    private static InboxState NeighborInbox(Guid input, Guid consumer) => new()
    {
        MessageId = input,
        ConsumerId = consumer,
        LockId = Guid.NewGuid(),
        Received = DateTimeOffset.UtcNow,
        ReceiveCount = 7,
    };

    private static OutboxMessage NeighborMessage(OutboxMessage source, Guid input, Guid consumer) => new()
    {
        InboxMessageId = input,
        InboxConsumerId = consumer,
        MessageId = Guid.NewGuid(),
        Body = source.Body,
        ContentType = source.ContentType,
        MessageType = source.MessageType,
        Headers = source.Headers,
        Properties = source.Properties,
        SentTime = source.SentTime,
        DestinationAddress = new Uri("loopback://localhost/untouched-neighbor"),
    };

    public sealed record Command(Guid Id);
    public sealed record Effect(Guid InputId, int Index);
    public sealed record Delivery(Effect Message, Guid? MessageId, string? Header);

    public sealed class CommandConsumer(RecoveryProbe probe) : IConsumer<Command>
    {
        public async Task ConsumeAsync(ConsumeContext<Command> context)
        {
            Interlocked.Increment(ref probe.ConsumerCalls);
            ISendEndpoint endpoint = await context.Advanced().GetSendEndpointAsync(new Uri("loopback://localhost/recovery-output"),
                cancellationToken: context.CancellationToken);
            for (int index = 0; index < 5; index++)
            {
                int current = index;
                await endpoint.SendAsync(new Effect(context.Message.Id, current), send =>
                {
                    send.MessageId = probe.EffectIds[current];
                    send.Headers.Set("recovery-header", $"effect-{current}");
                }, context.CancellationToken);
            }
        }
    }

    public sealed class EffectConsumer(RecoveryProbe probe) : IConsumer<Effect>
    {
        public Task ConsumeAsync(ConsumeContext<Effect> context)
        {
            probe.Deliveries.Enqueue(new Delivery(context.Message, context.MessageId,
                context.Headers.Get<string>("recovery-header")));
            probe.EffectArrived.Release();
            return Task.CompletedTask;
        }
    }

    public sealed class RecoveryDbContext(DbContextOptions<RecoveryDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }

    public sealed class ExpectedSendFailure : Exception;
    public sealed class ExpectedCleanupFailure : Exception;
    public sealed class ExpectedPersistenceFailure : Exception;

    public sealed class RecoveryProbe : DbCommandInterceptor, IDbTransactionInterceptor, ISaveChangesInterceptor, IReceiveObserver, ISendObserver
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseSend = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentDictionary<Guid, byte> _deleting = new();
        private int _consumptionObserved;
        private int _sendFailures;
        private int _cleanupFailures;
        private int _persistenceFailures;
        private Exception? _pendingFailure;
        public string? PersistencePhase { get; set; }
        public long? FailAfterSequence { get; set; }
        public Exception PersistenceFailure { get; } = new ExpectedPersistenceFailure();
        public int PersistenceFailures => Volatile.Read(ref _persistenceFailures);
        public bool FailCleanup { get; set; }
        public Exception CleanupFailure { get; } = new ExpectedCleanupFailure();
        public int CleanupFailures => Volatile.Read(ref _cleanupFailures);
        public Guid? FailSendId { get; set; }
        public string? PendingSendMode { get; set; }
        public TaskCompletionSource<CancellationToken> SendEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ReleaseSend() => _releaseSend.TrySetResult();
        public Exception SendFailure { get; } = new ExpectedSendFailure();
        public int SendFailures => Volatile.Read(ref _sendFailures);
        public Guid InputId { get; } = Guid.NewGuid();
        public Guid[] EffectIds { get; } = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        public int ConsumerCalls;
        public ConcurrentQueue<Delivery> Deliveries { get; } = new();
        public SemaphoreSlim EffectArrived { get; } = new(0);
        public TaskCompletionSource<Guid> ConsumptionCommitted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DrainCommitted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Exception> ReceiveFailure { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void ReleaseConsumption() => _release.TrySetResult();

        public async Task WaitForEffectsAsync(int count, TimeSpan timeout, CancellationToken token)
        {
            for (int index = 0; index < count; index++)
                Assert.True(await EffectArrived.WaitAsync(timeout, token), "Expected committed/replayed outgoing effect was not consumed.");
        }

        public async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not { } db)
                return;
            if (_deleting.TryRemove(db.ContextId.InstanceId, out _))
                DrainCommitted.TrySetResult();
            InboxState? consumed = db.ChangeTracker.Entries<InboxState>()
                .Select(x => x.Entity).SingleOrDefault(x => x.MessageId == InputId && x.Consumed.HasValue);
            if (consumed is not null && Interlocked.CompareExchange(ref _consumptionObserved, 1, 0) == 0)
            {
                ConsumptionCommitted.TrySetResult(consumed.ConsumerId);
                await _release.Task.WaitAsync(cancellationToken);
            }
        }

        private void FailPersistence(DbContext? db, string phase)
        {
            if (PersistencePhase == phase && FailAfterSequence.HasValue && db is not null
                && db.ChangeTracker.Entries<InboxState>().Any(x => x.Entity.MessageId == InputId
                    && x.Entity.LastSequenceNumber == FailAfterSequence && x.Entity.Consumed.HasValue)
                && Interlocked.CompareExchange(ref _persistenceFailures, 1, 0) == 0)
                throw PersistenceFailure;
        }

        public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FailPersistence(eventData.Context, "save");
            return ValueTask.FromResult(result);
        }

        public ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData,
            InterceptionResult result, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FailPersistence(eventData.Context, "commit");
            return ValueTask.FromResult(result);
        }

        private void ObserveDelete(DbCommand command, CommandEventData data)
        {
            if (data.Context is { } db && command.CommandText.Contains("DELETE FROM \"OutboxMessage\"", StringComparison.Ordinal))
            {
                if (FailCleanup && Interlocked.CompareExchange(ref _cleanupFailures, 1, 0) == 0)
                    throw CleanupFailure;
                _deleting.TryAdd(db.ContextId.InstanceId, 0);
            }
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObserveDelete(command, eventData);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObserveDelete(command, eventData);
            return ValueTask.FromResult(result);
        }

        public Task PreReceiveAsync(ReceiveContext context) => Task.CompletedTask;
        public async Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            if (context.MessageId != FailSendId || !FailSendId.HasValue
                || Interlocked.CompareExchange(ref _sendFailures, 1, 0) != 0)
                return;
            if (PendingSendMode is not null)
            {
                SendEntered.TrySetResult(context.CancellationToken);
                await _releaseSend.Task.WaitAsync(context.CancellationToken);
            }
            throw SendFailure;
        }
        public Task PostSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;
        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => Task.CompletedTask;
        public Task PostReceiveAsync(ReceiveContext context)
        {
            if (context.InputAddress.AbsolutePath.EndsWith("/recovery-input", StringComparison.Ordinal)
                && Volatile.Read(ref _pendingFailure) is { } failure)
                ReceiveFailure.TrySetResult(failure);
            return Task.CompletedTask;
        }
        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class => Task.CompletedTask;
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
        {
            if (typeof(T) == typeof(Command) && context.MessageId == InputId)
                Interlocked.CompareExchange(ref _pendingFailure, exception, null);
            return Task.CompletedTask;
        }
        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            if (context.InputAddress.AbsolutePath.EndsWith("/recovery-input", StringComparison.Ordinal))
            {
                Interlocked.CompareExchange(ref _pendingFailure, exception, null);
                if (PendingSendMode == "deadline" && exception is OperationCanceledException)
                    ReceiveFailure.TrySetResult(exception);
            }
            return Task.CompletedTask;
        }
    }

    private sealed class RecoveryFixture(PostgreSqlTestDatabase database, ServiceProvider provider, ITestHarness harness,
        RecoveryProbe probe, ConnectHandle observer, ConnectHandle sendObserver) : IAsyncDisposable
    {
        private bool _stopped;
        public RecoveryProbe Probe => probe;
        public TimeSpan Timeout => database.OperationTimeout;
        public RecoveryDbContext OpenDb() => new(new DbContextOptionsBuilder<RecoveryDbContext>().UseNpgsql(database.ConnectionString).Options);

        public static async Task<RecoveryFixture> CreateAsync(int limit, bool shortDeliveryTimeout = false)
        {
            CancellationToken token = TestContext.Current.CancellationToken;
            PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync("consumer-outbox-recovery", token);
            var probe = new RecoveryProbe();
            ServiceProvider? provider = null;
            try
            {
                var services = new ServiceCollection();
                services.AddSingleton(probe);
                services.AddDbContext<RecoveryDbContext>(builder => builder.UseNpgsql(database.ConnectionString).AddInterceptors(probe));
                services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
                {
                    configuration.SetTestTimeouts(database.OperationTimeout, database.OperationTimeout);
                    configuration.Contracts(contracts => contracts.Register<Command>("vicione.tests.recovery-input")
                        .Register<Effect>("vicione.tests.recovery-effect"));
                    configuration.ConfigureEntityFrameworkTransactionalStore<RecoveryDbContext>(outbox =>
                    {
                        outbox.UsePostgreSql();
                        outbox.DisableInboxCleanupService();
                    });
                    configuration.AddConsumer<CommandConsumer>();
                    configuration.AddConsumer<EffectConsumer>();
                    configuration.UsingInMemory((context, bus) =>
                    {
                        bus.ReceiveEndpoint("recovery-input", endpoint =>
                        {
                            endpoint.UseEntityFrameworkOutbox<RecoveryDbContext>(context, options =>
                            {
                                options.MessageDeliveryLimit = limit;
                                if (shortDeliveryTimeout)
                                    options.MessageDeliveryTimeout = TimeSpan.FromSeconds(5);
                            });
                            endpoint.ConfigureConsumer<CommandConsumer>(context);
                        });
                        bus.ReceiveEndpoint("recovery-output", endpoint => endpoint.ConfigureConsumer<EffectConsumer>(context));
                    });
                });
                await using (var db = new RecoveryDbContext(new DbContextOptionsBuilder<RecoveryDbContext>()
                                 .UseNpgsql(database.ConnectionString).Options))
                    Assert.True(await db.Database.EnsureCreatedAsync(token));
                provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
                ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: token).WaitAsync(database.OperationTimeout, token);
                ConnectHandle observer = harness.Bus.ConnectReceiveObserver(probe);
                ConnectHandle sendObserver = harness.Bus.ConnectSendObserver(probe);
                return new RecoveryFixture(database, provider, harness, probe, observer, sendObserver);
            }
            catch
            {
                probe.ReleaseConsumption();
                if (provider is not null)
                    await provider.DisposeAsync();
                await database.DisposeAsync();
                throw;
            }
        }

        public async Task SubmitAsync(CancellationToken token)
        {
            ISendEndpoint endpoint = await harness.Bus.GetSendEndpointAsync(new Uri("loopback://localhost/recovery-input"), cancellationToken: token);
            await endpoint.SendAsync(new Command(probe.InputId), context => context.MessageId = probe.InputId, token);
        }

        public async Task StopAsync()
        {
            if (_stopped)
                return;
            probe.ReleaseConsumption();
            probe.ReleaseSend();
            await harness.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            _stopped = true;
        }

        public async ValueTask DisposeAsync()
        {
            probe.ReleaseConsumption();
            try { await StopAsync(); }
            finally
            {
                observer.Disconnect();
                sendObserver.Disconnect();
                try { await provider.DisposeAsync(); }
                finally { await database.DisposeAsync(); probe.EffectArrived.Dispose(); }
            }
        }
    }
}
