using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.DurableSend;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class DurableSenderDeliveryTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-03T12:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-COMPLETION", "transport-acceptance-retires-immediately")]
    public async Task TransportAcceptance_RetiresThePersistedIntent()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new ControlledDispatcher(DurableSendDispatchResult.TransportAccepted);
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(store, dispatcher, time);
        await Admit(store);

        Assert.True(await driver.DeliverDueBatch(TestCancellationToken));

        Assert.Equal(1, dispatcher.DispatchCount);
        Assert.Equal(0, (await Snapshot(store)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-COMPLETION", "volatile-send-awaits-logical-consumer")]
    public async Task ConsumerCompletion_KeepsCapacityUntilTheLogicalConsumerCompletes()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new ControlledDispatcher(DurableSendDispatchResult.AwaitConsumerCompletion);
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(store, dispatcher, time);
        await Admit(store);

        Assert.True(await driver.DeliverDueBatch(TestCancellationToken));
        DurableSendStoreSnapshot waiting = await Snapshot(store);
        Assert.Equal(1, waiting.StoredCount);
        Assert.Equal(1, waiting.AwaitingConsumerCompletionCount);
        Assert.NotNull(dispatcher.LastCompletion);

        Assert.True(await dispatcher.LastCompletion!.CompleteAsync(TestCancellationToken));
        Assert.False(await dispatcher.LastCompletion.CompleteAsync(TestCancellationToken));
        Assert.Equal(0, (await Snapshot(store)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-RACE", "early-consumer-completion-wins-await-transition")]
    public async Task ConsumerCompletion_MayWinBeforeTheAwaitingStateTransition()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new ControlledDispatcher(DurableSendDispatchResult.AwaitConsumerCompletion)
        {
            CompleteBeforeReturn = true,
        };
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(store, dispatcher, time);
        await Admit(store);

        Assert.True(await driver.DeliverDueBatch(TestCancellationToken));

        Assert.Equal(1, dispatcher.DispatchCount);
        Assert.Equal(0, (await Snapshot(store)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-RACE", "completion-wins-ambiguous-dispatch-failure")]
    public async Task ConsumerCompletion_WinsAnOverlappingAmbiguousDispatchFailure()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new ControlledDispatcher(DurableSendDispatchResult.AwaitConsumerCompletion)
        {
            CompleteBeforeThrow = true,
        };
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(store, dispatcher, time);
        await Admit(store);

        Assert.True(await driver.DeliverDueBatch(TestCancellationToken));

        Assert.Equal(1, dispatcher.DispatchCount);
        Assert.Equal(0, (await Snapshot(store)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-TIMEOUT", "bounded-redispatch-then-quarantine")]
    public async Task MissingConsumerCompletion_RedispatchesOnlyWithinTheAttemptBudget()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new ControlledDispatcher(DurableSendDispatchResult.AwaitConsumerCompletion);
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(
            store,
            dispatcher,
            time,
            options =>
            {
                options.MaximumDeliveryAttempts = 2;
                options.ConsumerCompletionTimeout = TimeSpan.FromMinutes(5);
            });
        await Admit(store);

        await driver.DeliverDueBatch(TestCancellationToken);
        time.Advance(TimeSpan.FromMinutes(5));
        await driver.DeliverDueBatch(TestCancellationToken);
        time.Advance(TimeSpan.FromMinutes(5));
        await driver.DeliverDueBatch(TestCancellationToken);

        Assert.Equal(2, dispatcher.DispatchCount);
        DurableSendQuarantineEntry evidence = Assert.Single(await Quarantine(store));
        Assert.Equal(DurableSendFailureKind.ConsumerCompletionTimeout, evidence.FailureKind);
        Assert.Equal(2, evidence.DeliveryAttempts);
        Assert.Equal("consumer-completion-timeout", evidence.FailureType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-RACE", "late-completion-resolves-timeout-quarantine")]
    public async Task LateConsumerCompletion_RetiresTheSameGenerationAfterTimeoutQuarantine()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new ControlledDispatcher(DurableSendDispatchResult.AwaitConsumerCompletion);
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(
            store,
            dispatcher,
            time,
            options =>
            {
                options.MaximumDeliveryAttempts = 1;
                options.ConsumerCompletionTimeout = TimeSpan.FromMinutes(5);
            });
        await Admit(store);

        await driver.DeliverDueBatch(TestCancellationToken);
        IDurableSendConsumerCompletion completion = Assert.IsAssignableFrom<IDurableSendConsumerCompletion>(
            dispatcher.LastCompletion);
        time.Advance(TimeSpan.FromMinutes(5));
        await driver.DeliverDueBatch(TestCancellationToken);
        Assert.Single(await Quarantine(store));

        Assert.True(await completion.CompleteAsync(TestCancellationToken));
        Assert.Equal(0, (await Snapshot(store)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-FAILURE", "classified-retry-and-terminal-evidence")]
    public async Task TransportFailure_OnlyClassifiedTransientFailuresRetryAndExhaustionQuarantines()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new ThrowingDispatcher(new ExpectedDispatchException());
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(
            store,
            dispatcher,
            time,
            options =>
            {
                options.MaximumDeliveryAttempts = 2;
                options.InitialRetryDelay = TimeSpan.FromMinutes(1);
                options.MaximumRetryDelay = TimeSpan.FromMinutes(1);
                options.RetryJitterFraction = 0;
            },
            [new ConstantClassifier(TransportSendFailureKind.Transient)]);
        await Admit(store);

        await driver.DeliverDueBatch(TestCancellationToken);
        DurableSendStoreSnapshot retry = await Snapshot(store);
        Assert.Equal(1, retry.RetryScheduledCount);
        Assert.False(await driver.DeliverDueBatch(TestCancellationToken));
        time.Advance(TimeSpan.FromMinutes(1));
        Assert.True(await driver.DeliverDueBatch(TestCancellationToken));

        Assert.Equal(2, dispatcher.DispatchCount);
        DurableSendQuarantineEntry terminal = Assert.Single(await Quarantine(store));
        Assert.Equal(DurableSendFailureKind.RetryLimitExceeded, terminal.FailureKind);
        Assert.Equal(2, terminal.DeliveryAttempts);
        Assert.EndsWith(nameof(ExpectedDispatchException), terminal.FailureType, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-FAILURE", "permanent-unknown-and-invalid-classification-fail-closed")]
    public async Task TransportFailure_PermanentAndUnknownClassificationsNeverEnterRetry()
    {
        DurableSendFailureKind permanent = await DeliverOneFailure(
            [new ConstantClassifier(TransportSendFailureKind.Permanent)]);
        DurableSendFailureKind explicitUnknown = await DeliverOneFailure(
            [new ConstantClassifier(TransportSendFailureKind.Unclassified)]);
        DurableSendFailureKind invalid = await DeliverOneFailure(
            [new ConstantClassifier((TransportSendFailureKind)999)]);
        DurableSendFailureKind absent = await DeliverOneFailure([]);
        DurableSendFailureKind afterBrokenClassifier = await DeliverOneFailure(
            [new ThrowingClassifier(), new ConstantClassifier(TransportSendFailureKind.Transient)],
            maximumAttempts: 1);

        Assert.Equal(DurableSendFailureKind.NonRetryable, permanent);
        Assert.Equal(DurableSendFailureKind.Unclassified, explicitUnknown);
        Assert.Equal(DurableSendFailureKind.Unclassified, invalid);
        Assert.Equal(DurableSendFailureKind.Unclassified, absent);
        Assert.Equal(DurableSendFailureKind.RetryLimitExceeded, afterBrokenClassifier);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-INVARIANT", "unsupported-completion-mode-quarantines")]
    public async Task UnsupportedCompletionMode_FailsClosedIntoInvariantQuarantine()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new ControlledDispatcher(new DurableSendDispatchResult((DurableSendCompletionMode)999));
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(store, dispatcher, time);
        await Admit(store);

        await driver.DeliverDueBatch(TestCancellationToken);

        DurableSendQuarantineEntry evidence = Assert.Single(await Quarantine(store));
        Assert.Equal(DurableSendFailureKind.InvariantViolation, evidence.FailureKind);
        Assert.Equal("invalid-durable-send-completion-mode", evidence.FailureType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-PERSISTENCE", "successful-dispatch-state-failure-propagates")]
    public async Task StatePersistenceFailure_AfterSuccessfulDispatchEscapesWithoutDeletingTheIntent()
    {
        IDurableSendStore<ITestBus> inner = Store();
        var expected = new ExpectedPersistenceException();
        var store = new MarkDeliveredThrowingStore(inner, expected);
        var dispatcher = new ControlledDispatcher(DurableSendDispatchResult.TransportAccepted);
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(store, dispatcher, time);
        await Admit(store);

        ExpectedPersistenceException actual = await Assert.ThrowsAsync<ExpectedPersistenceException>(() =>
            driver.DeliverDueBatch(TestCancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, dispatcher.DispatchCount);
        Assert.Equal(1, (await Snapshot(inner)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-CONCURRENCY", "claim-never-exceeds-immediate-worker-capacity")]
    public async Task BatchClaim_StartsNoMoreThanTheConfiguredConcurrentDeliveryLimit()
    {
        IDurableSendStore<ITestBus> store = Store();
        var dispatcher = new BlockingDispatcher(expectedConcurrent: 2);
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(
            store,
            dispatcher,
            time,
            options => options.MaximumConcurrentDeliveries = 2);
        await Admit(store, 1);
        await Admit(store, 2);
        await Admit(store, 3);

        Task<bool> firstBatch = driver.DeliverDueBatch(TestCancellationToken);
        await dispatcher.ExpectedConcurrentEntered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestCancellationToken);
        Assert.Equal(2, dispatcher.DispatchCount);
        Assert.Equal(2, dispatcher.MaximumConcurrent);
        Assert.Equal(3, (await Snapshot(store)).StoredCount);

        dispatcher.Release.TrySetResult();
        Assert.True(await firstBatch);
        Assert.Equal(1, (await Snapshot(store)).StoredCount);
        Assert.True(await driver.DeliverDueBatch(TestCancellationToken));
        Assert.Equal(3, dispatcher.DispatchCount);
        Assert.Equal(0, (await Snapshot(store)).StoredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-DELIVERY-RETRY", "deterministic-decorrelated-ceiling-jitter")]
    public void RetryDelay_RemainsBoundedAndDecorrelatedAtTheBackoffCeiling()
    {
        IDurableSendStore<ITestBus> store = Store();
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(
            store,
            new ControlledDispatcher(DurableSendDispatchResult.TransportAccepted),
            time,
            options =>
            {
                options.InitialRetryDelay = TimeSpan.FromSeconds(15);
                options.MaximumRetryDelay = TimeSpan.FromMinutes(5);
                options.RetryJitterFraction = 0.20;
            });

        TimeSpan[] first = Enumerable.Range(1, 32)
            .Select(value => driver.CalculateRetryDelay(new DurableSendId(GuidFrom(value)), attempt: 20))
            .ToArray();
        TimeSpan[] second = Enumerable.Range(1, 32)
            .Select(value => driver.CalculateRetryDelay(new DurableSendId(GuidFrom(value)), attempt: 20))
            .ToArray();

        Assert.Equal(first, second);
        Assert.All(first, delay => Assert.InRange(delay, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5)));
        Assert.True(first.Distinct().Count() > 1);
        Assert.Contains(first, delay => delay < TimeSpan.FromMinutes(5));
    }

    private static async Task<DurableSendFailureKind> DeliverOneFailure(
        IEnumerable<ITransportSendFailureClassifier> classifiers,
        int maximumAttempts = 3)
    {
        IDurableSendStore<ITestBus> store = Store();
        var time = new FakeTimeProvider(Epoch);
        using DurableSenderDeliveryTestDriver<ITestBus> driver = Driver(
            store,
            new ThrowingDispatcher(new ExpectedDispatchException()),
            time,
            options => options.MaximumDeliveryAttempts = maximumAttempts,
            classifiers);
        await Admit(store);
        await driver.DeliverDueBatch(TestCancellationToken);
        return Assert.Single(await Quarantine(store)).FailureKind;
    }

    private static DurableSenderDeliveryTestDriver<ITestBus> Driver(
        IDurableSendStore<ITestBus> store,
        IDurableSendDispatcher<ITestBus> dispatcher,
        TimeProvider timeProvider,
        Action<DurableSenderOptions<ITestBus>>? configure = null,
        IEnumerable<ITransportSendFailureClassifier>? classifiers = null) =>
        DurableSenderTestFactory.CreateDeliveryDriver(store, dispatcher, timeProvider, configure, classifiers);

    private static async Task<SerializedDurableSend> Admit(IDurableSendStore<ITestBus> store, int id = 1)
    {
        SerializedDurableSend message = Message(id);
        await store.AdmitAsync(
            message,
            new DurableSendStoreLimits(100, 1_000_000),
            Epoch,
            TestCancellationToken);
        return message;
    }

    private static Task<DurableSendStoreSnapshot> Snapshot(IDurableSendStore<ITestBus> store) =>
        store.GetSnapshotAsync(TestCancellationToken);

    private static Task<IReadOnlyList<DurableSendQuarantineEntry>> Quarantine(IDurableSendStore<ITestBus> store) =>
        store.GetQuarantineAsync(10, TestCancellationToken);

    private static SerializedDurableSend Message(int id) => new()
    {
        Id = new DurableSendId(GuidFrom(id)),
        ContractIdentity = new MessageContractIdentity("vicione.tests.durable-delivery", 1),
        DestinationAddress = new Uri("loopback://durable-delivery"),
        ContentType = "application/octet-stream",
        Body = new byte[] { 1, 2, 3 },
    };

    private static Guid GuidFrom(int value) => new(value, 0, 0, new byte[8]);

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static IDurableSendStore<ITestBus> Store() =>
        DurableSenderTestFactory.CreateInMemoryStore<ITestBus>();

    private interface ITestBus : IBus;

    private sealed class ControlledDispatcher(DurableSendDispatchResult result) : IDurableSendDispatcher<ITestBus>
    {
        public bool CompleteBeforeReturn { get; init; }
        public bool CompleteBeforeThrow { get; init; }
        public int DispatchCount { get; private set; }
        public IDurableSendConsumerCompletion? LastCompletion { get; private set; }

        public async Task<DurableSendDispatchResult> DispatchAsync(
            DurableSendDispatchContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DispatchCount++;
            LastCompletion = context.ConsumerCompletion;
            if (CompleteBeforeReturn || CompleteBeforeThrow)
                await context.ConsumerCompletion.CompleteAsync(cancellationToken);
            if (CompleteBeforeThrow)
                throw new ExpectedAmbiguousDispatchException();
            return result;
        }
    }

    private sealed class ThrowingDispatcher(Exception exception) : IDurableSendDispatcher<ITestBus>
    {
        public int DispatchCount { get; private set; }

        public Task<DurableSendDispatchResult> DispatchAsync(
            DurableSendDispatchContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            cancellationToken.ThrowIfCancellationRequested();
            DispatchCount++;
            return Task.FromException<DurableSendDispatchResult>(exception);
        }
    }

    private sealed class BlockingDispatcher(int expectedConcurrent) : IDurableSendDispatcher<ITestBus>
    {
        private readonly TaskCompletionSource _expectedConcurrentEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _dispatchCount;
        private int _inside;
        private int _maximumConcurrent;

        public int DispatchCount => Volatile.Read(ref _dispatchCount);
        public int MaximumConcurrent => Volatile.Read(ref _maximumConcurrent);
        public TaskCompletionSource ExpectedConcurrentEntered => _expectedConcurrentEntered;
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<DurableSendDispatchResult> DispatchAsync(
            DurableSendDispatchContext context,
            CancellationToken cancellationToken = default)
        {
            _ = context;
            int total = Interlocked.Increment(ref _dispatchCount);
            int inside = Interlocked.Increment(ref _inside);
            ObserveMaximum(inside);
            if (total == expectedConcurrent)
                _expectedConcurrentEntered.TrySetResult();

            try
            {
                await Release.Task.WaitAsync(cancellationToken);
                return DurableSendDispatchResult.TransportAccepted;
            }
            finally
            {
                Interlocked.Decrement(ref _inside);
            }
        }

        private void ObserveMaximum(int current)
        {
            int observed;
            do
            {
                observed = Volatile.Read(ref _maximumConcurrent);
                if (observed >= current)
                    return;
            }
            while (Interlocked.CompareExchange(ref _maximumConcurrent, current, observed) != observed);
        }
    }

    private sealed class ConstantClassifier(TransportSendFailureKind kind) : ITransportSendFailureClassifier
    {
        public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
        {
            _ = exception;
            failureKind = kind;
            return true;
        }
    }

    private sealed class ThrowingClassifier : ITransportSendFailureClassifier
    {
        public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
        {
            _ = exception;
            failureKind = default;
            throw new ExpectedClassifierException();
        }
    }

    private sealed class MarkDeliveredThrowingStore(
        IDurableSendStore<ITestBus> inner,
        Exception exception) : IDurableSendStore<ITestBus>
    {
        public Task<DurableSendAdmissionResult> AdmitAsync(
            SerializedDurableSend message,
            DurableSendStoreLimits limits,
            DateTimeOffset enqueuedAt,
            CancellationToken cancellationToken = default) =>
            inner.AdmitAsync(message, limits, enqueuedAt, cancellationToken);

        public Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
            DateTimeOffset now,
            int maximumCount,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default) =>
            inner.ClaimDueAsync(now, maximumCount, leaseDuration, cancellationToken);

        public Task<bool> MarkDeliveredAsync(
            DurableSendId id,
            DurableSendLease lease,
            DateTimeOffset deliveredAt,
            CancellationToken cancellationToken = default) =>
            Task.FromException<bool>(exception);

        public Task<bool> AwaitConsumerCompletionAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DateTimeOffset nextAttemptAt,
            CancellationToken cancellationToken = default) =>
            inner.AwaitConsumerCompletionAsync(id, lease, deliveryAttempts, nextAttemptAt, cancellationToken);

        public Task<bool> CompleteConsumerDeliveryAsync(
            DurableSendId id,
            Guid generationToken,
            DateTimeOffset completedAt,
            CancellationToken cancellationToken = default) =>
            inner.CompleteConsumerDeliveryAsync(id, generationToken, completedAt, cancellationToken);

        public Task<bool> ScheduleRetryAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DateTimeOffset nextAttemptAt,
            DurableSendFailureKind failureKind,
            string? failureType,
            DateTimeOffset failedAt,
            CancellationToken cancellationToken = default) =>
            inner.ScheduleRetryAsync(
                id,
                lease,
                deliveryAttempts,
                nextAttemptAt,
                failureKind,
                failureType,
                failedAt,
                cancellationToken);

        public Task<bool> QuarantineAsync(
            DurableSendId id,
            DurableSendLease lease,
            int deliveryAttempts,
            DurableSendFailureKind failureKind,
            string? failureType,
            DateTimeOffset quarantinedAt,
            CancellationToken cancellationToken = default) =>
            inner.QuarantineAsync(
                id,
                lease,
                deliveryAttempts,
                failureKind,
                failureType,
                quarantinedAt,
                cancellationToken);

        public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) =>
            inner.GetSnapshotAsync(cancellationToken);

        public Task<IReadOnlyList<DurableSendQuarantineEntry>> GetQuarantineAsync(
            int maximumCount,
            CancellationToken cancellationToken = default) =>
            inner.GetQuarantineAsync(maximumCount, cancellationToken);

        public Task<bool> RequeueAsync(
            DurableSendId id,
            DateTimeOffset dueAt,
            CancellationToken cancellationToken = default) =>
            inner.RequeueAsync(id, dueAt, cancellationToken);

        public Task<bool> DiscardQuarantinedAsync(
            DurableSendId id,
            CancellationToken cancellationToken = default) =>
            inner.DiscardQuarantinedAsync(id, cancellationToken);
    }

    private sealed class ExpectedDispatchException : Exception;
    private sealed class ExpectedPersistenceException : Exception;
    private sealed class ExpectedClassifierException : Exception;
    private sealed class ExpectedAmbiguousDispatchException : Exception;
}
