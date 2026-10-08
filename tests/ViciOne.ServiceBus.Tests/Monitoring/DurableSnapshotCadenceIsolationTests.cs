using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class DurableSnapshotCadenceIsolationTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "durable-snapshot-optional-utc-preserves-real-batch-result")]
    public async Task Snapshot_PreservesCompletedBatchAsync(bool hasMessage, bool failOptionalUtc)
    {
        var clock = new SnapshotClock(Epoch);
        IOutboxStore<IBus> inner = DurableSenderTestFactory.CreateInMemoryStore<IBus>();
        SerializedDurableSend? message = hasMessage ? await AdmitAsync(inner) : null;
        var store = new SnapshotStore(inner, clock);
        if (failOptionalUtc)
        {
            if (hasMessage) store.AfterMark = () => clock.FailNextRead = true;
            else store.AfterClaim = () => clock.FailNextRead = true;
        }
        var dispatcher = new SnapshotDispatcher();
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, dispatcher, clock);
        bool result = false;
        Exception? escaped = await Record.ExceptionAsync(async () => result = await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, store.Claims);
        Assert.Equal(hasMessage ? 1 : 0, dispatcher.Calls);
        Assert.Equal(hasMessage ? 1 : 0, store.Marks);
        if (hasMessage)
        {
            Assert.True(store.MarkResult);
            Assert.Equal(message!.Id, store.MarkedId);
            Assert.Equal(Epoch, store.MarkedAt);
            Assert.Equal(Assert.Single(store.LastClaims).Lease, store.MarkedLease);
            Assert.Equal(TestContext.Current.CancellationToken, store.MarkedToken);
        }
        DurableSendStoreSnapshot actual = await inner.GetSnapshotAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, actual.StoredCount);
        Assert.Equal(0L, actual.StoredBytes);
        Assert.Null(escaped);
        Assert.Equal(hasMessage, result);
        Assert.Equal(failOptionalUtc ? 0 : 1, store.Snapshots);
        Assert.Equal(failOptionalUtc ? 1 : 0, clock.Failures);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "durable-snapshot-unrepresentable-successor-is-bounded")]
    public async Task Snapshot_PositiveMaximumIntervalDoesNotEscapeOrRepeatAsync(bool hasMessage)
    {
        var clock = new SnapshotClock(Epoch);
        IOutboxStore<IBus> inner = DurableSenderTestFactory.CreateInMemoryStore<IBus>();
        if (hasMessage) await AdmitAsync(inner);
        var store = new SnapshotStore(inner, clock);
        var dispatcher = new SnapshotDispatcher();
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, dispatcher, clock,
            options => options.TelemetrySnapshotInterval = TimeSpan.MaxValue);
        bool result = false;
        Exception? escaped = await Record.ExceptionAsync(async () => result = await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(hasMessage ? 1 : 0, store.Marks);
        Assert.Equal(hasMessage ? 1 : 0, dispatcher.Calls);
        Assert.Equal(0, (await inner.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);
        Assert.Null(escaped);
        Assert.Equal(hasMessage, result);
        Assert.Equal(1, store.Snapshots);
        int reads = clock.Reads;
        for (int i = 0; i < 3; i++)
            Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, store.Snapshots);
        Assert.Equal(reads + 3, clock.Reads); // Only the required claim read remains.
    }

    [Fact]
    public async Task Snapshot_ExactMaximumDeadlineIsObservedOnceAsync()
    {
        var clock = new SnapshotClock(DateTimeOffset.MaxValue - Interval);
        var store = new SnapshotStore(DurableSenderTestFactory.CreateInMemoryStore<IBus>(), clock);
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, new SnapshotDispatcher(), clock);
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, store.Snapshots);
        clock.Now = DateTimeOffset.MaxValue;
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, store.Snapshots);
        int reads = clock.Reads;
        for (int i = 0; i < 3; i++)
            Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, store.Snapshots);
        Assert.Equal(reads + 3, clock.Reads);
    }

    [Theory]
    [InlineData(SnapshotFailure.Utc)]
    [InlineData(SnapshotFailure.Store)]
    [InlineData(SnapshotFailure.Cancellation)]
    public async Task Snapshot_FailedObservationReservesCadenceBeforeRetryAsync(SnapshotFailure failure)
    {
        var clock = new SnapshotClock(Epoch);
        var store = new SnapshotStore(DurableSenderTestFactory.CreateInMemoryStore<IBus>(), clock);
        if (failure == SnapshotFailure.Utc) store.AfterClaim = () => clock.FailNextRead = true;
        else store.SnapshotFailure = failure == SnapshotFailure.Cancellation
            ? new OperationCanceledException(TestContext.Current.CancellationToken) : new InvalidOperationException("optional snapshot store failed");
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, new SnapshotDispatcher(), clock);
        Exception? escaped = await Record.ExceptionAsync(async () => Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(1, store.Claims);
        Assert.Null(escaped);
        Assert.Equal(failure == SnapshotFailure.Utc ? 0 : 1, store.Snapshots);
        Assert.Equal(failure == SnapshotFailure.Utc ? 1 : 0, clock.Failures);
        store.AfterClaim = null;
        store.SnapshotFailure = null;
        int reads = clock.Reads;
        clock.Now = Epoch + Interval - TimeSpan.FromTicks(1);
        for (int i = 0; i < 3; i++)
            Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(reads + 3, clock.Reads);
        Assert.Equal(failure == SnapshotFailure.Utc ? 0 : 1, store.Snapshots);
        clock.Now = Epoch + Interval;
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(failure == SnapshotFailure.Utc ? 1 : 2, store.Snapshots);
        Assert.Equal(reads + 5, clock.Reads);
    }

    [Fact]
    public async Task Snapshot_SlowDeliveryPreservesMinimumObservationIntervalAsync()
    {
        var clock = new SnapshotClock(Epoch);
        IOutboxStore<IBus> inner = DurableSenderTestFactory.CreateInMemoryStore<IBus>();
        await AdmitAsync(inner);
        var store = new SnapshotStore(inner, clock);
        var dispatcher = new SnapshotDispatcher() { AfterDispatch = () => clock.Now = Epoch.AddSeconds(4) };
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, dispatcher, clock);
        Assert.True(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.True(store.MarkResult);
        Assert.Equal(Epoch.AddSeconds(4), store.MarkedAt);
        Assert.Equal(0, (await inner.GetSnapshotAsync(TestContext.Current.CancellationToken)).StoredCount);
        Assert.Equal(1, store.Snapshots);
        int reads = clock.Reads;
        clock.Now = Epoch.AddSeconds(5);
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        clock.Now = Epoch.AddSeconds(9).AddTicks(-1);
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, store.Snapshots);
        Assert.Equal(reads + 2, clock.Reads);
        clock.Now = Epoch.AddSeconds(9);
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, store.Snapshots);
    }

    [Fact]
    public async Task Snapshot_BackwardsObservationDoesNotShortenReservationAsync()
    {
        var clock = new SnapshotClock(Epoch);
        var store = new SnapshotStore(DurableSenderTestFactory.CreateInMemoryStore<IBus>(), clock)
        { AfterClaim = () => clock.Now = Epoch.AddSeconds(-3) };
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, new SnapshotDispatcher(), clock);
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, store.Snapshots);
        store.AfterClaim = null;
        clock.Now = Epoch.AddSeconds(2);
        int reads = clock.Reads;
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, store.Snapshots);
        Assert.Equal(reads + 1, clock.Reads);
        clock.Now = Epoch.AddSeconds(5);
        Assert.False(await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, store.Snapshots);
    }

    [Fact]
    public async Task Snapshot_RequiredClaimClockFailureRemainsAuthoritativeAsync()
    {
        var clock = new SnapshotClock(Epoch) { FailNextRead = true };
        var store = new SnapshotStore(DurableSenderTestFactory.CreateInMemoryStore<IBus>(), clock);
        var dispatcher = new SnapshotDispatcher();
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, dispatcher, clock);
        Exception? escaped = await Record.ExceptionAsync(async () => await driver.DeliverDueBatchAsync(TestContext.Current.CancellationToken));
        Assert.Same(clock.Failure, escaped);
        Assert.Equal(0, store.Claims);
        Assert.Equal(0, store.Snapshots);
        Assert.Equal(0, store.Marks);
        Assert.Equal(0, dispatcher.Calls);
    }

    private static async Task<SerializedDurableSend> AdmitAsync(IOutboxStore<IBus> store)
    {
        var message = new SerializedDurableSend
        {
            Id = new DurableSendId(Guid.NewGuid()), ContractIdentity = new MessageContractIdentity("snapshot-cadence", 1),
            DestinationAddress = new Uri("loopback://localhost/snapshot-cadence"), ContentType = "application/json",
            Body = new byte[] { 1, 2, 3 },
        };
        Assert.Equal(DurableSendAdmissionDisposition.Accepted,
            (await store.AdmitAsync(message, new DurableSendStoreLimits(2, 10), Epoch, TestContext.Current.CancellationToken)).Disposition);
        return message;
    }

    public enum SnapshotFailure { Utc, Store, Cancellation }

    private sealed class SnapshotClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public bool FailNextRead { get; set; }
        public int Reads { get; private set; }
        public int Failures { get; private set; }
        public Exception Failure { get; } = new InvalidOperationException("optional snapshot UTC failed");
        public override DateTimeOffset GetUtcNow()
        {
            Reads++;
            if (!FailNextRead) return Now;
            FailNextRead = false;
            Failures++;
            throw Failure;
        }
    }

    private sealed class SnapshotDispatcher : IDurableSendDispatcher<IBus>
    {
        public int Calls { get; private set; }
        public Action? AfterDispatch { get; set; }
        public Task<DurableSendDispatchResult> DispatchAsync(DurableSendDispatchContext context, CancellationToken cancellationToken = default)
        {
            Calls++;
            Assert.Equal(context.Message.Id, context.DurableSendId);
            Assert.Equal(context.Message.Id, context.ConsumerCompletion.DurableSendId);
            Assert.Equal(1, context.Attempt);
            Assert.Equal(TestContext.Current.CancellationToken, cancellationToken);
            AfterDispatch?.Invoke();
            return Task.FromResult(DurableSendDispatchResult.TransportAccepted);
        }
    }

    private sealed class SnapshotStore(IOutboxStore<IBus> inner, SnapshotClock clock) : IOutboxStore<IBus>
    {
        public int Claims { get; private set; }
        public int Marks { get; private set; }
        public int Snapshots { get; private set; }
        public Action? AfterClaim { get; set; }
        public Action? AfterMark { get; set; }
        public Exception? SnapshotFailure { get; set; }
        public IReadOnlyList<DurableSendDelivery> LastClaims { get; private set; } = [];
        public bool MarkResult { get; private set; }
        public DurableSendId MarkedId { get; private set; }
        public DurableSendLease MarkedLease { get; private set; }
        public DateTimeOffset MarkedAt { get; private set; }
        public CancellationToken MarkedToken { get; private set; }
        public async Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(DateTimeOffset now, int maximumCount, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            Assert.Equal(clock.Now, now);
            Assert.Equal(16, maximumCount);
            Assert.Equal(TimeSpan.FromMinutes(2), leaseDuration);
            Assert.Equal(TestContext.Current.CancellationToken, cancellationToken);
            Claims++;
            LastClaims = await inner.ClaimDueAsync(now, maximumCount, leaseDuration, cancellationToken);
            AfterClaim?.Invoke();
            return LastClaims;
        }
        public async Task<bool> MarkDeliveredAsync(DurableSendId id, DurableSendLease lease, DateTimeOffset deliveredAt, CancellationToken cancellationToken = default)
        {
            Marks++;
            MarkedId = id; MarkedLease = lease; MarkedAt = deliveredAt; MarkedToken = cancellationToken;
            MarkResult = await inner.MarkDeliveredAsync(id, lease, deliveredAt, cancellationToken);
            AfterMark?.Invoke();
            return MarkResult;
        }
        public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            Snapshots++;
            return SnapshotFailure is { } error ? Task.FromException<DurableSendStoreSnapshot>(error) : inner.GetSnapshotAsync(cancellationToken);
        }
        public Task<bool> AwaitConsumerCompletionAsync(DurableSendId id, DurableSendLease lease, int deliveryAttempts, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ScheduleRetryAsync(DurableSendId id, DurableSendLease lease, int deliveryAttempts, DateTimeOffset nextAttemptAt,
            DurableSendFailureKind failureKind, string? failureType, DateTimeOffset failedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> QuarantineAsync(DurableSendId id, DurableSendLease lease, int deliveryAttempts, DurableSendFailureKind failureKind,
            string? failureType, DateTimeOffset quarantinedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> CompleteConsumerDeliveryAsync(DurableSendId id, Guid generationToken, DateTimeOffset completedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DurableSendAdmissionResult> AdmitAsync(SerializedDurableSend message, DurableSendStoreLimits limits, DateTimeOffset enqueuedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DurableSendQuarantinePage> GetQuarantineAsync(DurableSendQuarantineQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DurableSendOperationResult> RequeueAsync(DurableSendId id, DateTimeOffset dueAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DurableSendOperationResult> DiscardQuarantinedAsync(DurableSendId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
