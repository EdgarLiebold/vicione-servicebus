using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Monitoring.Telemetry;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class DurableConsumerCompletionClockIsolationTests
{
    private static readonly DateTimeOffset Epoch = new(2026, 10, 7, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(DiagnosticFailure.None, false)]
    [InlineData(DiagnosticFailure.None, true)]
    [InlineData(DiagnosticFailure.InitialTimestamp, false)]
    [InlineData(DiagnosticFailure.InitialTimestamp, true)]
    [InlineData(DiagnosticFailure.ElapsedTimestamp, false)]
    [InlineData(DiagnosticFailure.ElapsedTimestamp, true)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "durable-completion-optional-clock-keeps-store-result")]
    public async Task Completion_PreservesActualStoreResultWhenDiagnosticClockThrowsAsync(
        DiagnosticFailure diagnosticFailure, bool staleGeneration)
    {
        var clock = new CompletionClock(diagnosticFailure);
        IOutboxStore<IBus> inner = DurableSenderTestFactory.CreateInMemoryStore<IBus>();
        DurableSendDelivery delivery = await AdmitAndClaimAsync(inner);
        Guid generation = staleGeneration ? Guid.NewGuid() : delivery.GenerationToken;
        Assert.NotEqual(Guid.Empty, generation);
        if (staleGeneration)
            Assert.NotEqual(delivery.GenerationToken, generation);
        IOutboxStore<IBus> store = DispatchProxy.Create<IOutboxStore<IBus>, CompletionStoreProxy>();
        var proxy = (CompletionStoreProxy)store;
        proxy.Inner = inner;
        using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var instrumentation = new ServiceBusInstrumentation<IBus>(services.GetRequiredService<IMeterFactory>());
        using var observations = new MetricObservationSession(services.GetRequiredService<IMeterFactory>());
        IDurableSendConsumerCompletion? subject = null;
        Exception? creation = Record.Exception(() => subject = new DurableSendConsumerCompletion<IBus>(
            delivery.Message.Id, generation, store, clock, instrumentation));
        Assert.Equal(0, proxy.Calls);
        await AssertRetainedAsync(inner, retained: true);
        Assert.Null(creation);
        Assert.NotNull(subject);
        Assert.Equal(delivery.Message.Id, subject.DurableSendId);
        clock.Advance(TimeSpan.FromSeconds(7));
        using var operation = new CancellationTokenSource();
        bool retired = false;
        Exception? escaped = await Record.ExceptionAsync(async () => retired = await subject.CompleteAsync(operation.Token));

        AssertCall(proxy, delivery.Message.Id, generation, operation.Token);
        await AssertRetainedAsync(inner, retained: staleGeneration);
        Assert.Null(escaped);
        Assert.Equal(!staleGeneration, retired);
        MetricMeasurement counter = Assert.Single(observations.Measurements,
            value => value.Name == ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletion);
        Assert.Equal(1d, counter.Value);
        AssertOutcome(counter, staleGeneration ? "not-retired" : "retired");
        MetricMeasurement[] durations = observations.Measurements
            .Where(value => value.Name == ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletionDuration).ToArray();
        if (diagnosticFailure == DiagnosticFailure.None)
        {
            MetricMeasurement duration = Assert.Single(durations);
            Assert.Equal(7d, duration.Value);
            AssertOutcome(duration, staleGeneration ? "not-retired" : "retired");
        }
        else
            Assert.Empty(durations);
        Assert.Equal(diagnosticFailure == DiagnosticFailure.InitialTimestamp ? 1 : 2, clock.TimestampReads);
    }

    [Theory]
    [InlineData(RequiredFailure.Utc)]
    [InlineData(RequiredFailure.Store)]
    [InlineData(RequiredFailure.Cancellation)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "durable-completion-required-failures-remain-authoritative")]
    public async Task Completion_PreservesRequiredClockStoreAndCancellationFailuresAsync(RequiredFailure failure)
    {
        var required = new InvalidOperationException("required completion boundary failed");
        var clock = new CompletionClock(DiagnosticFailure.None);
        IOutboxStore<IBus> inner = DurableSenderTestFactory.CreateInMemoryStore<IBus>();
        DurableSendDelivery delivery = await AdmitAndClaimAsync(inner);
        IOutboxStore<IBus> store = DispatchProxy.Create<IOutboxStore<IBus>, CompletionStoreProxy>();
        var proxy = (CompletionStoreProxy)store;
        proxy.Inner = inner;
        using var services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var instrumentation = new ServiceBusInstrumentation<IBus>(services.GetRequiredService<IMeterFactory>());
        using var observations = new MetricObservationSession(services.GetRequiredService<IMeterFactory>());
        IDurableSendConsumerCompletion subject = new DurableSendConsumerCompletion<IBus>(
            delivery.Message.Id, delivery.GenerationToken, store, clock, instrumentation);
        clock.Advance(TimeSpan.FromSeconds(7));
        using var operation = new CancellationTokenSource();
        if (failure == RequiredFailure.Utc)
            clock.UtcFailure = required;
        else if (failure == RequiredFailure.Store)
            proxy.Failure = required;
        else
            operation.Cancel();
        Exception? escaped = await Record.ExceptionAsync(async () => await subject.CompleteAsync(operation.Token));

        if (failure == RequiredFailure.Cancellation)
        {
            OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(escaped);
            Assert.Equal(operation.Token, canceled.CancellationToken);
        }
        else
            Assert.Same(required, escaped);
        if (failure == RequiredFailure.Utc)
            Assert.Equal(0, proxy.Calls);
        else
            AssertCall(proxy, delivery.Message.Id, delivery.GenerationToken, operation.Token);
        await AssertRetainedAsync(inner, retained: true);
        Assert.Empty(observations.Measurements);
        Assert.Equal(1, clock.TimestampReads);
    }

    private static async Task<DurableSendDelivery> AdmitAndClaimAsync(IOutboxStore<IBus> store)
    {
        var message = new SerializedDurableSend
        {
            Id = new DurableSendId(Guid.NewGuid()),
            ContractIdentity = new MessageContractIdentity("completion-clock-item", 1),
            DestinationAddress = new Uri("loopback://localhost/completion-clock"),
            ContentType = "application/json",
            Body = new byte[] { 1, 2, 3 },
        };
        DurableSendAdmissionResult admitted = await store.AdmitAsync(message, new DurableSendStoreLimits(2, 10), Epoch);
        Assert.Equal(DurableSendAdmissionDisposition.Accepted, admitted.Disposition);
        DurableSendDelivery delivery = Assert.Single(await store.ClaimDueAsync(Epoch, 1, TimeSpan.FromMinutes(1)));
        Assert.Equal(message.Id, delivery.Message.Id);
        Assert.NotEqual(Guid.Empty, delivery.GenerationToken);
        await AssertRetainedAsync(store, retained: true);
        return delivery;
    }

    private static async Task AssertRetainedAsync(IOutboxStore<IBus> store, bool retained)
    {
        DurableSendStoreSnapshot snapshot = await store.GetSnapshotAsync();
        Assert.Equal(retained ? 1 : 0, snapshot.StoredCount);
        Assert.Equal(retained ? 3L : 0L, snapshot.StoredBytes);
    }

    private static void AssertCall(CompletionStoreProxy proxy, DurableSendId id, Guid generation, CancellationToken token)
    {
        Assert.Equal(1, proxy.Calls);
        Assert.Equal(id, proxy.Id);
        Assert.Equal(generation, proxy.Generation);
        Assert.Equal(Epoch.AddSeconds(7), proxy.CompletedAt);
        Assert.Equal(token, proxy.Token);
    }

    private static void AssertOutcome(MetricMeasurement measurement, string outcome)
    {
        KeyValuePair<string, object?> tag = Assert.Single(measurement.Tags);
        Assert.Equal(ServiceBusTelemetry.Attributes.Outcome, tag.Key);
        Assert.Equal(outcome, tag.Value);
    }

    public enum DiagnosticFailure { None, InitialTimestamp, ElapsedTimestamp }
    public enum RequiredFailure { Utc, Store, Cancellation }

    private sealed class CompletionClock(DiagnosticFailure failure) : TimeProvider
    {
        private readonly FakeTimeProvider _inner = new(Epoch);
        public int TimestampReads { get; private set; }
        public Exception? UtcFailure { get; set; }
        public override long TimestampFrequency => _inner.TimestampFrequency;
        public override DateTimeOffset GetUtcNow() => UtcFailure is { } error ? throw error : _inner.GetUtcNow();
        public override long GetTimestamp()
        {
            TimestampReads++;
            if (failure == DiagnosticFailure.InitialTimestamp && TimestampReads == 1
                || failure == DiagnosticFailure.ElapsedTimestamp && TimestampReads == 2)
                throw new InvalidOperationException("optional completion timestamp failed");
            return _inner.GetTimestamp();
        }
        public void Advance(TimeSpan elapsed) => _inner.Advance(elapsed);
    }

    public class CompletionStoreProxy : DispatchProxy
    {
        public IOutboxStore<IBus> Inner { get; set; } = null!;
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }
        public DurableSendId Id { get; private set; }
        public Guid Generation { get; private set; }
        public DateTimeOffset CompletedAt { get; private set; }
        public CancellationToken Token { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.Equal(nameof(IOutboxStore<IBus>.CompleteConsumerDeliveryAsync), targetMethod?.Name);
            Assert.NotNull(args);
            Assert.Equal(4, args.Length);
            Calls++;
            Id = Assert.IsType<DurableSendId>(args[0]);
            Generation = Assert.IsType<Guid>(args[1]);
            CompletedAt = Assert.IsType<DateTimeOffset>(args[2]);
            Token = Assert.IsType<CancellationToken>(args[3]);
            return Failure is { } error ? Task.FromException<bool>(error)
                : Inner.CompleteConsumerDeliveryAsync(Id, Generation, CompletedAt, Token);
        }
    }
}
