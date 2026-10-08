using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class DurableDeliveryClockIsolationTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

    public static TheoryData<DiagnosticFailure, Scenario> DeliveryCases
    {
        get
        {
            var cases = new TheoryData<DiagnosticFailure, Scenario>();
            foreach (DiagnosticFailure failure in Enum.GetValues<DiagnosticFailure>())
            foreach (Scenario scenario in Enum.GetValues<Scenario>())
                cases.Add(failure, scenario);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(DeliveryCases))]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "durable-delivery-clock-keeps-real-outcome-and-state")]
    public async Task Delivery_PreservesActualOutcomesWhenDiagnosticClockThrowsAsync(DiagnosticFailure failure, Scenario scenario)
    {
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var clock = new DeliveryClock(failure);
        IOutboxStore<IBus> inner = DurableSenderTestFactory.CreateInMemoryStore<IBus>();
        SerializedDurableSend message = await AdmitAsync(inner, IsTimeout(scenario));
        var store = new ObservedStore(inner, clock, operation, scenario);
        var dispatcher = new ObservedDispatcher(clock, operation, scenario);
        var classifier = new ObservedClassifier(scenario, dispatcher.Failure);
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, dispatcher, clock,
            options =>
            {
                options.RetryJitterFraction = 0;
                options.MaximumDeliveryAttempts = IsTimeout(scenario) || scenario == Scenario.RetryExhausted ? 1 : 10;
            }, [classifier]);
        using var observations = new MetricObservationSession(driver.MeterScope);
        bool didWork = false;
        Exception? escaped = await Record.ExceptionAsync(async () => didWork = await driver.DeliverDueBatchAsync(operation.Token));

        if (ReferenceEquals(clock.InitialFailure, escaped))
        {
            Assert.Equal(1, store.Claims);
            Assert.Equal(0, dispatcher.Calls);
            Assert.Empty(store.Transitions);
            DurableSendStoreSnapshot beforeDispatch = await inner.GetSnapshotAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, beforeDispatch.StoredCount);
            Assert.Equal(3L, beforeDispatch.StoredBytes);
            Assert.Null(escaped);
        }
        Assert.Equal(1, store.Claims);
        DurableSendDelivery claimed = Assert.IsType<DurableSendDelivery>(store.Claimed);
        Assert.Equal(message.Id, claimed.Message.Id);
        Assert.Equal(new byte[] { 1, 2, 3 }, claimed.Message.Body.ToArray());
        Assert.NotEqual(Guid.Empty, claimed.GenerationToken);
        Assert.NotEqual(Guid.Empty, claimed.Lease.Token);
        Assert.Equal(IsTimeout(scenario) ? 0 : 1, dispatcher.Calls);
        if (!IsTimeout(scenario))
        {
            Assert.Equal(message.Id, dispatcher.Context.DurableSendId);
            Assert.Equal(message.Id, dispatcher.Context.ConsumerCompletion.DurableSendId);
            Assert.Equal(claimed.Message, dispatcher.Context.Message);
            Assert.Equal(1, dispatcher.Context.Attempt);
            Assert.Equal(operation.Token, dispatcher.Token);
        }
        Assert.Equal(IsDispatchFailure(scenario) ? 1 : 0, classifier.Calls);
        if (IsDispatchFailure(scenario))
            Assert.Same(dispatcher.Failure, classifier.Seen);
        Assert.Equal(IsRace(scenario) ? 1 : 0, store.Completions);
        Assert.Equal(scenario == Scenario.DispatchCancellation ? 0 : 1, store.Transitions.Count);
        if (scenario != Scenario.DispatchCancellation)
        {
            AssertTransition(Assert.Single(store.Transitions), claimed, scenario, operation.Token);
            bool? expectedTransitionResult = IsPersistenceFailure(scenario) || IsCancellation(scenario)
                ? null : !IsRace(scenario);
            Assert.Equal(expectedTransitionResult, store.TransitionResult);
        }

        DurableSendStoreSnapshot snapshot = await inner.GetSnapshotAsync(TestContext.Current.CancellationToken);
        bool removed = scenario == Scenario.TransportAccepted || IsRace(scenario);
        bool quarantined = scenario is Scenario.ClassifiedPermanent or Scenario.Unclassified or Scenario.RetryExhausted
            or Scenario.InvalidMode or Scenario.ConsumerTimeout;
        bool awaiting = scenario == Scenario.AwaitConsumerCompletion
            || IsTimeout(scenario) && scenario != Scenario.ConsumerTimeout;
        Assert.Equal(removed ? 0 : 1, snapshot.StoredCount);
        Assert.Equal(removed ? 0L : 3L, snapshot.StoredBytes);
        Assert.Equal(quarantined ? 1 : 0, snapshot.QuarantinedCount);
        Assert.Equal(scenario == Scenario.ClassifiedTransient ? 1 : 0, snapshot.RetryScheduledCount);
        Assert.Equal(awaiting ? 1 : 0, snapshot.AwaitingConsumerCompletionCount);
        Assert.Equal(removed || quarantined ? 0 : 1, snapshot.PendingCount);
        if (IsPersistenceFailure(scenario))
            Assert.Same(store.PersistenceFailure, escaped);
        else if (IsCancellation(scenario))
        {
            OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(escaped);
            Assert.Equal(operation.Token, canceled.CancellationToken);
        }
        else
        {
            Assert.Null(escaped);
            Assert.True(didWork);
        }
        AssertMetrics(observations, ExpectedOutcome(scenario), ExpectedError(scenario),
            failure == DiagnosticFailure.None ? 7d : null);
    }

    [Theory]
    [InlineData(UtcBoundary.Claim)]
    [InlineData(UtcBoundary.Mark)]
    [InlineData(UtcBoundary.Await)]
    [InlineData(UtcBoundary.InvalidMode)]
    [InlineData(UtcBoundary.ClassifiedFailure)]
    [InlineData(UtcBoundary.Timeout)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "durable-delivery-required-utc-boundaries-stay-authoritative")]
    public async Task Delivery_PreservesRequiredUtcFailuresAsync(UtcBoundary boundary)
    {
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var required = new InvalidOperationException("required delivery UTC failed");
        var clock = new DeliveryClock(DiagnosticFailure.None);
        Scenario scenario = boundary switch
        {
            UtcBoundary.Await => Scenario.AwaitConsumerCompletion,
            UtcBoundary.InvalidMode => Scenario.InvalidMode,
            UtcBoundary.ClassifiedFailure => Scenario.ClassifiedPermanent,
            UtcBoundary.Timeout => Scenario.ConsumerTimeout,
            _ => Scenario.TransportAccepted,
        };
        IOutboxStore<IBus> inner = DurableSenderTestFactory.CreateInMemoryStore<IBus>();
        SerializedDurableSend message = await AdmitAsync(inner, IsTimeout(scenario));
        var store = new ObservedStore(inner, clock, operation, scenario);
        var dispatcher = new ObservedDispatcher(clock, operation, scenario);
        var classifier = new ObservedClassifier(scenario, dispatcher.Failure);
        if (boundary == UtcBoundary.Claim)
            clock.UtcFailure = required;
        else if (boundary == UtcBoundary.Timeout)
            store.AfterClaim = () => clock.UtcFailure = required;
        else
            dispatcher.AfterDispatch = () => clock.UtcFailure = required;
        using var driver = DurableSenderTestFactory.CreateDeliveryDriver(store, dispatcher, clock,
            options => options.MaximumDeliveryAttempts = IsTimeout(scenario) ? 1 : 10, [classifier]);
        using var observations = new MetricObservationSession(driver.MeterScope);
        Exception? escaped = await Record.ExceptionAsync(async () => await driver.DeliverDueBatchAsync(operation.Token));

        Assert.Same(required, escaped);
        Assert.Equal(boundary == UtcBoundary.Claim ? 0 : 1, store.Claims);
        Assert.Equal(boundary is UtcBoundary.Claim or UtcBoundary.Timeout ? 0 : 1, dispatcher.Calls);
        Assert.Equal(0, classifier.Calls); // Required UTC is read before classification.
        Assert.Empty(store.Transitions);
        Assert.Equal(0, store.Completions);
        DurableSendStoreSnapshot snapshot = await inner.GetSnapshotAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, snapshot.StoredCount);
        Assert.Equal(3L, snapshot.StoredBytes);
        Assert.Equal(1, snapshot.PendingCount);
        Assert.Equal(0, snapshot.QuarantinedCount);
        Assert.Equal(0, snapshot.RetryScheduledCount);
        Assert.Equal(boundary == UtcBoundary.Timeout ? 1 : 0, snapshot.AwaitingConsumerCompletionCount);
        if (boundary == UtcBoundary.Claim)
            Assert.Empty(observations.Measurements);
        else
        {
            Assert.Equal(message.Id, Assert.IsType<DurableSendDelivery>(store.Claimed).Message.Id);
            AssertMetrics(observations, "state-persistence-failed", "durable-state-persistence-failure",
                boundary == UtcBoundary.Timeout ? 0d : 7d);
        }
    }

    private static async Task<SerializedDurableSend> AdmitAsync(IOutboxStore<IBus> store, bool timeout)
    {
        var message = new SerializedDurableSend
        {
            Id = new DurableSendId(Guid.NewGuid()),
            ContractIdentity = new MessageContractIdentity("delivery-clock-item", 1),
            DestinationAddress = new Uri("loopback://localhost/delivery-clock"),
            ContentType = "application/json",
            Body = new byte[] { 1, 2, 3 },
        };
        DateTimeOffset admittedAt = timeout ? Epoch.AddMinutes(-1) : Epoch;
        DurableSendAdmissionResult result = await store.AdmitAsync(message, new DurableSendStoreLimits(2, 10), admittedAt);
        Assert.Equal(DurableSendAdmissionDisposition.Accepted, result.Disposition);
        if (timeout)
        {
            DurableSendDelivery delivery = Assert.Single(await store.ClaimDueAsync(admittedAt, 1, TimeSpan.FromMinutes(1)));
            Assert.True(await store.AwaitConsumerCompletionAsync(message.Id, delivery.Lease, 1, Epoch));
        }
        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync();
        Assert.Equal(1, snapshot.StoredCount);
        Assert.Equal(3L, snapshot.StoredBytes);
        Assert.Equal(timeout ? 1 : 0, snapshot.AwaitingConsumerCompletionCount);
        return message;
    }

    private static void AssertTransition(Transition transition, DurableSendDelivery claimed, Scenario scenario, CancellationToken token)
    {
        Assert.Equal(claimed.Message.Id, transition.Id);
        Assert.Equal(claimed.Lease, transition.Lease);
        Assert.Equal(token, transition.Token);
        DateTimeOffset now = IsTimeout(scenario) ? Epoch : Epoch.AddSeconds(7);
        string method = scenario switch
        {
            Scenario.AwaitConsumerCompletion or Scenario.CompleteBeforeAwait => "await",
            Scenario.ClassifiedTransient or Scenario.CompleteBeforeRetry => "retry",
            Scenario.TransportAccepted or Scenario.PersistenceFailure or Scenario.StateCancellation or Scenario.CompleteBeforeMark => "mark",
            _ => "quarantine",
        };
        Assert.Equal(method, transition.Kind);
        DateTimeOffset? timestamp = method == "await" ? null : now;
        DateTimeOffset? next = method == "await" ? now.AddMinutes(5) : method == "retry" ? now.AddSeconds(15) : null;
        int? attempt = method == "mark" ? null : 1;
        Assert.Equal(timestamp, transition.Timestamp);
        Assert.Equal(next, transition.NextAttemptAt);
        Assert.Equal(attempt, transition.Attempt);
        DurableSendFailureKind? kind = method switch
        {
            "retry" => DurableSendFailureKind.Transient,
            "quarantine" when IsTimeout(scenario) => DurableSendFailureKind.ConsumerCompletionTimeout,
            "quarantine" when scenario == Scenario.InvalidMode => DurableSendFailureKind.InvariantViolation,
            "quarantine" when scenario == Scenario.RetryExhausted => DurableSendFailureKind.RetryLimitExceeded,
            "quarantine" when scenario == Scenario.Unclassified => DurableSendFailureKind.Unclassified,
            "quarantine" => DurableSendFailureKind.NonRetryable,
            _ => null,
        };
        Assert.Equal(kind, transition.FailureKind);
        Assert.Equal(kind switch
        {
            null => null,
            DurableSendFailureKind.ConsumerCompletionTimeout => "consumer-completion-timeout",
            DurableSendFailureKind.InvariantViolation => "invalid-durable-send-completion-mode",
            _ => typeof(DispatchFailure).FullName,
        }, transition.FailureType);
    }

    private static void AssertMetrics(MetricObservationSession observations, string outcome, string? error, double? seconds)
    {
        MetricMeasurement counter = Assert.Single(observations.Measurements,
            value => value.Name == ServiceBusTelemetry.Metrics.DurableSenderDelivery);
        Assert.Equal(1d, counter.Value);
        AssertTags(counter, outcome, error);
        MetricMeasurement[] durations = observations.Measurements
            .Where(value => value.Name == ServiceBusTelemetry.Metrics.DurableSenderDeliveryDuration).ToArray();
        if (seconds is { } known)
        {
            MetricMeasurement duration = Assert.Single(durations);
            Assert.Equal(known, duration.Value);
            AssertTags(duration, outcome, error);
        }
        else
            Assert.Empty(durations);
    }

    private static void AssertTags(MetricMeasurement measurement, string outcome, string? error)
    {
        Assert.Equal(error is null ? 1 : 2, measurement.Tags.Count);
        Assert.Equal(outcome, measurement.Tag(ServiceBusTelemetry.Attributes.Outcome));
        if (error is not null)
            Assert.Equal(error, measurement.Tag(ServiceBusTelemetry.Attributes.ErrorType));
    }

    private static bool IsTimeout(Scenario value) => value is Scenario.ConsumerTimeout or Scenario.TimeoutPersistenceFailure or Scenario.TimeoutCancellation;
    private static bool IsRace(Scenario value) => value is Scenario.CompleteBeforeMark or Scenario.CompleteBeforeAwait or Scenario.CompleteBeforeRetry or Scenario.CompleteBeforeQuarantine;
    private static bool IsPersistenceFailure(Scenario value) => value is Scenario.PersistenceFailure or Scenario.FailurePersistenceFailure or Scenario.TimeoutPersistenceFailure;
    private static bool IsCancellation(Scenario value) => value is Scenario.DispatchCancellation or Scenario.StateCancellation or Scenario.FailurePersistenceCancellation or Scenario.TimeoutCancellation;
    private static bool IsDispatchFailure(Scenario value) => value is Scenario.ClassifiedTransient or Scenario.ClassifiedPermanent or Scenario.Unclassified or Scenario.RetryExhausted
        or Scenario.FailurePersistenceFailure or Scenario.FailurePersistenceCancellation or Scenario.CompleteBeforeRetry or Scenario.CompleteBeforeQuarantine;
    private static string ExpectedOutcome(Scenario value) => value switch
    {
        _ when IsPersistenceFailure(value) => "state-persistence-failed",
        _ when IsCancellation(value) => "canceled",
        Scenario.ClassifiedTransient => "retry-scheduled",
        Scenario.ClassifiedPermanent or Scenario.Unclassified or Scenario.RetryExhausted or Scenario.InvalidMode or Scenario.ConsumerTimeout => "quarantined",
        Scenario.AwaitConsumerCompletion => "awaiting-consumer-completion",
        _ => "delivered",
    };
    private static string? ExpectedError(Scenario value) => value switch
    {
        _ when IsPersistenceFailure(value) => "durable-state-persistence-failure",
        Scenario.ClassifiedTransient => "transient",
        Scenario.ClassifiedPermanent => "non-retryable",
        Scenario.Unclassified => "unclassified",
        Scenario.RetryExhausted => "retry-limit-exceeded",
        Scenario.InvalidMode => "invariant-violation",
        Scenario.ConsumerTimeout => "consumer-completion-timeout",
        _ => null,
    };

    public enum DiagnosticFailure { None, InitialTimestamp, ElapsedFrequency }
    public enum UtcBoundary { Claim, Mark, Await, InvalidMode, ClassifiedFailure, Timeout }
    public enum Scenario
    {
        TransportAccepted, AwaitConsumerCompletion, ClassifiedTransient, ClassifiedPermanent, Unclassified,
        RetryExhausted, InvalidMode, PersistenceFailure, DispatchCancellation, StateCancellation,
        FailurePersistenceFailure, FailurePersistenceCancellation, ConsumerTimeout, TimeoutPersistenceFailure,
        TimeoutCancellation, CompleteBeforeMark, CompleteBeforeAwait, CompleteBeforeRetry, CompleteBeforeQuarantine,
    }

    private sealed class DeliveryClock(DiagnosticFailure failure) : TimeProvider
    {
        private readonly FakeTimeProvider _inner = new(Epoch);
        private int _reads;
        public Exception InitialFailure { get; } = new InvalidOperationException("optional delivery initial timestamp failed");
        public Exception? UtcFailure { get; set; }
        public override DateTimeOffset GetUtcNow() => UtcFailure is { } error ? throw error : _inner.GetUtcNow();
        public override long TimestampFrequency => failure == DiagnosticFailure.ElapsedFrequency
            ? throw new InvalidOperationException("optional delivery duration frequency failed") : _inner.TimestampFrequency;
        public override long GetTimestamp()
        {
            if (++_reads == 1 && failure == DiagnosticFailure.InitialTimestamp)
                throw InitialFailure;
            return _inner.GetTimestamp();
        }
        public void Advance() => _inner.Advance(TimeSpan.FromSeconds(7));
    }

    private sealed class DispatchFailure : Exception;

    private sealed class ObservedDispatcher(DeliveryClock clock, CancellationTokenSource operation, Scenario scenario) : IDurableSendDispatcher<IBus>
    {
        public DispatchFailure Failure { get; } = new();
        public int Calls { get; private set; }
        public DurableSendDispatchContext Context { get; private set; }
        public CancellationToken Token { get; private set; }
        public Action? AfterDispatch { get; set; }
        public async Task<DurableSendDispatchResult> DispatchAsync(DurableSendDispatchContext context, CancellationToken cancellationToken = default)
        {
            Calls++;
            Context = context;
            Token = cancellationToken;
            clock.Advance();
            AfterDispatch?.Invoke();
            if (IsRace(scenario))
                Assert.True(await context.ConsumerCompletion.CompleteAsync(cancellationToken));
            if (scenario == Scenario.DispatchCancellation)
            {
                operation.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (IsDispatchFailure(scenario))
                throw Failure;
            return scenario switch
            {
                Scenario.AwaitConsumerCompletion or Scenario.CompleteBeforeAwait => DurableSendDispatchResult.AwaitConsumerCompletion,
                Scenario.InvalidMode => default,
                _ => DurableSendDispatchResult.TransportAccepted,
            };
        }
    }

    private sealed class ObservedClassifier(Scenario scenario, Exception expected) : ITransportSendFailureClassifier
    {
        public int Calls { get; private set; }
        public Exception? Seen { get; private set; }
        public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
        {
            Assert.Same(expected, exception);
            Calls++;
            Seen = exception;
            failureKind = scenario is Scenario.ClassifiedTransient or Scenario.RetryExhausted or Scenario.CompleteBeforeRetry
                ? TransportSendFailureKind.Transient : TransportSendFailureKind.Permanent;
            return scenario != Scenario.Unclassified;
        }
    }

    private sealed record Transition(string Kind, DurableSendId Id, DurableSendLease Lease, int? Attempt,
        DateTimeOffset? Timestamp, DateTimeOffset? NextAttemptAt, DurableSendFailureKind? FailureKind, string? FailureType, CancellationToken Token);

    private sealed class ObservedStore(IOutboxStore<IBus> inner, DeliveryClock clock, CancellationTokenSource operation, Scenario scenario) : IOutboxStore<IBus>
    {
        public Exception PersistenceFailure { get; } = new InvalidOperationException("required durable state persistence failed");
        public int Claims { get; private set; }
        public int Completions { get; private set; }
        public DurableSendDelivery? Claimed { get; private set; }
        public List<Transition> Transitions { get; } = [];
        public bool? TransitionResult { get; private set; }
        public Action? AfterClaim { get; set; }
        public async Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(DateTimeOffset now, int maximumCount, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            Assert.Equal(Epoch, now);
            Assert.Equal(16, maximumCount);
            Assert.Equal(TimeSpan.FromMinutes(2), leaseDuration);
            Assert.Equal(operation.Token, cancellationToken);
            Claims++;
            IReadOnlyList<DurableSendDelivery> result = await inner.ClaimDueAsync(now, maximumCount, leaseDuration, cancellationToken);
            Claimed = Assert.Single(result);
            AfterClaim?.Invoke();
            return result;
        }
        private async Task<bool> TransitionAsync(Transition transition, Func<Task<bool>> apply)
        {
            Transitions.Add(transition);
            if (IsTimeout(scenario))
                clock.Advance();
            if (IsPersistenceFailure(scenario))
                throw PersistenceFailure;
            if (IsCancellation(scenario))
                operation.Cancel();
            TransitionResult = await apply();
            return TransitionResult.Value;
        }
        public Task<bool> MarkDeliveredAsync(DurableSendId id, DurableSendLease lease, DateTimeOffset deliveredAt, CancellationToken cancellationToken = default) =>
            TransitionAsync(new("mark", id, lease, null, deliveredAt, null, null, null, cancellationToken),
                () => inner.MarkDeliveredAsync(id, lease, deliveredAt, cancellationToken));
        public Task<bool> AwaitConsumerCompletionAsync(DurableSendId id, DurableSendLease lease, int deliveryAttempts, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken = default) =>
            TransitionAsync(new("await", id, lease, deliveryAttempts, null, nextAttemptAt, null, null, cancellationToken),
                () => inner.AwaitConsumerCompletionAsync(id, lease, deliveryAttempts, nextAttemptAt, cancellationToken));
        public Task<bool> ScheduleRetryAsync(DurableSendId id, DurableSendLease lease, int deliveryAttempts, DateTimeOffset nextAttemptAt,
            DurableSendFailureKind failureKind, string? failureType, DateTimeOffset failedAt, CancellationToken cancellationToken = default) =>
            TransitionAsync(new("retry", id, lease, deliveryAttempts, failedAt, nextAttemptAt, failureKind, failureType, cancellationToken),
                () => inner.ScheduleRetryAsync(id, lease, deliveryAttempts, nextAttemptAt, failureKind, failureType, failedAt, cancellationToken));
        public Task<bool> QuarantineAsync(DurableSendId id, DurableSendLease lease, int deliveryAttempts, DurableSendFailureKind failureKind,
            string? failureType, DateTimeOffset quarantinedAt, CancellationToken cancellationToken = default) =>
            TransitionAsync(new("quarantine", id, lease, deliveryAttempts, quarantinedAt, null, failureKind, failureType, cancellationToken),
                () => inner.QuarantineAsync(id, lease, deliveryAttempts, failureKind, failureType, quarantinedAt, cancellationToken));
        public Task<bool> CompleteConsumerDeliveryAsync(DurableSendId id, Guid generationToken, DateTimeOffset completedAt, CancellationToken cancellationToken = default)
        {
            Completions++;
            DurableSendDelivery claimed = Assert.IsType<DurableSendDelivery>(Claimed);
            Assert.Equal(claimed.Message.Id, id);
            Assert.Equal(claimed.GenerationToken, generationToken);
            Assert.Equal(Epoch.AddSeconds(7), completedAt);
            Assert.Equal(operation.Token, cancellationToken);
            return inner.CompleteConsumerDeliveryAsync(id, generationToken, completedAt, cancellationToken);
        }
        public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default) => inner.GetSnapshotAsync(cancellationToken);
        public Task<DurableSendAdmissionResult> AdmitAsync(SerializedDurableSend message, DurableSendStoreLimits limits, DateTimeOffset enqueuedAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DurableSendQuarantinePage> GetQuarantineAsync(DurableSendQuarantineQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DurableSendOperationResult> RequeueAsync(DurableSendId id, DateTimeOffset dueAt, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<DurableSendOperationResult> DiscardQuarantinedAsync(DurableSendId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
