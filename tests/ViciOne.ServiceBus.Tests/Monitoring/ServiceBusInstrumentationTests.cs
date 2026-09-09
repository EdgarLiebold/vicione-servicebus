using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Monitoring.Telemetry;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class ServiceBusInstrumentationTests
{
    private static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-09-09T08:00:00+00:00");

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-INSTRUMENTS", "durable-payload-and-reliability-complete-schema")]
    public void MetricMethods_PublishTheCompleteDurablePayloadAndReliabilitySchema()
    {
        using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        IMeterFactory meterFactory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(meterFactory);
        using var instrumentation = new ServiceBusInstrumentation<IBus>(meterFactory);

        foreach (DurableSendAdmissionDisposition disposition in Enum.GetValues<DurableSendAdmissionDisposition>())
            instrumentation.RecordDurableAdmission(disposition, 100 + (int)disposition);
        instrumentation.RecordDurableAdmission((DurableSendAdmissionDisposition)int.MaxValue, 199);
        foreach (DurableSendAdmissionFailure failure in Enum.GetValues<DurableSendAdmissionFailure>())
            instrumentation.RecordDurableAdmissionRejected(failure, 200 + (int)failure);
        instrumentation.RecordDurableAdmissionRejected((DurableSendAdmissionFailure)int.MaxValue, 299);

        foreach (DurableSendDeliveryOutcome outcome in Enum.GetValues<DurableSendDeliveryOutcome>())
            instrumentation.RecordDurableDelivery(outcome, failureKind: null, 0.25 + (int)outcome);
        instrumentation.RecordDurableDelivery((DurableSendDeliveryOutcome)int.MaxValue, failureKind: null, 7.25);
        foreach (DurableSendFailureKind failure in Enum.GetValues<DurableSendFailureKind>())
            instrumentation.RecordDurableDelivery(DurableSendDeliveryOutcome.RetryScheduled, failure, 10.25 + (int)failure);
        instrumentation.RecordDurableDelivery(
            DurableSendDeliveryOutcome.RetryScheduled,
            (DurableSendFailureKind)int.MaxValue,
            99.25);

        instrumentation.RecordDurableConsumerCompletion(retired: true, 0.5);
        instrumentation.RecordDurableConsumerCompletion(retired: false, 1.5);
        instrumentation.RecordReliabilityAbandoned(ReliableMessageKind.Inbox);
        instrumentation.RecordReliabilityAbandoned(ReliableMessageKind.Outbox);
        instrumentation.RecordPayloadBody(PayloadAdmissionDisposition.Inline, 300, warningThresholdExceeded: false);
        instrumentation.RecordPayloadBody(PayloadAdmissionDisposition.OffloadToMessageData, 301, warningThresholdExceeded: true);
        foreach (PayloadAdmissionStage stage in Enum.GetValues<PayloadAdmissionStage>())
            instrumentation.RecordPayloadRejected(stage, 400 + (int)stage);
        instrumentation.RecordPayloadRejected((PayloadAdmissionStage)int.MaxValue, 499);
        instrumentation.RecordPayloadEnvelope(500, rejected: false);
        instrumentation.RecordPayloadEnvelope(501, rejected: true);

        instrumentation.PublishDurableSnapshot(
            new DurableSendStoreSnapshot(
                StoredCount: 11,
                StoredBytes: 12,
                PendingCount: 13,
                RetryScheduledCount: 14,
                AwaitingConsumerCompletionCount: 15,
                QuarantinedCount: 16,
                OldestPendingEnqueuedAt: Epoch.AddSeconds(-17)),
            Epoch);
        observations.RecordObservableInstruments();

        string[] expectedInstruments =
        [
            ServiceBusTelemetry.Metrics.DurableSenderAdmission,
            ServiceBusTelemetry.Metrics.DurableSenderAdmissionSize,
            ServiceBusTelemetry.Metrics.DurableSenderDelivery,
            ServiceBusTelemetry.Metrics.DurableSenderDeliveryDuration,
            ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletion,
            ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletionDuration,
            ServiceBusTelemetry.Metrics.ReliabilityAbandoned,
            ServiceBusTelemetry.Metrics.DurableSenderStored,
            ServiceBusTelemetry.Metrics.DurableSenderStoredContentSize,
            ServiceBusTelemetry.Metrics.DurableSenderPending,
            ServiceBusTelemetry.Metrics.DurableSenderRetryScheduled,
            ServiceBusTelemetry.Metrics.DurableSenderAwaitingConsumerCompletion,
            ServiceBusTelemetry.Metrics.DurableSenderQuarantined,
            ServiceBusTelemetry.Metrics.DurableSenderOldestPendingAge,
            ServiceBusTelemetry.Metrics.PayloadAdmission,
            ServiceBusTelemetry.Metrics.PayloadBodySize,
            ServiceBusTelemetry.Metrics.PayloadEnvelopeSize,
        ];
        Assert.Equal(
            expectedInstruments.Order(StringComparer.Ordinal),
            observations.Instruments.Select(instrument => instrument.Name).Order(StringComparer.Ordinal));
        Assert.All(observations.Instruments, instrument => Assert.False(string.IsNullOrWhiteSpace(instrument.Description)));
        AssertInstrument<Counter<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderAdmission, unit: null);
        AssertInstrument<Histogram<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderAdmissionSize, "By");
        AssertInstrument<Counter<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderDelivery, unit: null);
        AssertInstrument<Histogram<double>>(observations, ServiceBusTelemetry.Metrics.DurableSenderDeliveryDuration, "s");
        AssertInstrument<Counter<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletion, unit: null);
        AssertInstrument<Histogram<double>>(observations, ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletionDuration, "s");
        AssertInstrument<Counter<long>>(observations, ServiceBusTelemetry.Metrics.ReliabilityAbandoned, unit: null);
        AssertInstrument<ObservableGauge<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderStored, unit: null);
        AssertInstrument<ObservableGauge<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderStoredContentSize, "By");
        AssertInstrument<ObservableGauge<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderPending, unit: null);
        AssertInstrument<ObservableGauge<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderRetryScheduled, unit: null);
        AssertInstrument<ObservableGauge<long>>(
            observations,
            ServiceBusTelemetry.Metrics.DurableSenderAwaitingConsumerCompletion,
            unit: null);
        AssertInstrument<ObservableGauge<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderQuarantined, unit: null);
        AssertInstrument<ObservableGauge<long>>(observations, ServiceBusTelemetry.Metrics.DurableSenderOldestPendingAge, "s");
        AssertInstrument<Counter<long>>(observations, ServiceBusTelemetry.Metrics.PayloadAdmission, unit: null);
        AssertInstrument<Histogram<long>>(observations, ServiceBusTelemetry.Metrics.PayloadBodySize, "By");
        AssertInstrument<Histogram<long>>(observations, ServiceBusTelemetry.Metrics.PayloadEnvelopeSize, "By");

        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.DurableSenderAdmission,
            ServiceBusTelemetry.Attributes.Outcome,
            "accepted",
            "already-accepted",
            "already-quarantined",
            "unknown",
            "rejected");
        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.DurableSenderAdmission,
            ServiceBusTelemetry.Attributes.ErrorType,
            "capacity-exceeded",
            "identity-conflict",
            "contract-not-registered",
            "durable-store-failure",
            "admission-failed");
        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.DurableSenderDelivery,
            ServiceBusTelemetry.Attributes.Outcome,
            "delivered",
            "awaiting-consumer-completion",
            "retry-scheduled",
            "quarantined",
            "canceled",
            "state-persistence-failed",
            "unknown");
        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.DurableSenderDelivery,
            ServiceBusTelemetry.Attributes.ErrorType,
            "durable-state-persistence-failure",
            "none",
            "transient",
            "non-retryable",
            "unclassified",
            "retry-limit-exceeded",
            "invariant-violation",
            "consumer-completion-timeout",
            "unknown");
        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletion,
            ServiceBusTelemetry.Attributes.Outcome,
            "retired",
            "not-retired");
        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.ReliabilityAbandoned,
            ServiceBusTelemetry.Attributes.ReliabilitySide,
            "inbox",
            "outbox");
        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.PayloadAdmission,
            ServiceBusTelemetry.Attributes.Outcome,
            "inline",
            "message-data",
            "rejected");
        MetricMeasurement inlinePayload = Assert.Single(observations.Measurements, measurement =>
            measurement.Name == ServiceBusTelemetry.Metrics.PayloadAdmission
            && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.Outcome), "inline"));
        MetricMeasurement offloadedPayload = Assert.Single(observations.Measurements, measurement =>
            measurement.Name == ServiceBusTelemetry.Metrics.PayloadAdmission
            && Equals(measurement.Tag(ServiceBusTelemetry.Attributes.Outcome), "message-data"));
        Assert.False(Assert.IsType<bool>(
            inlinePayload.Tag(ServiceBusTelemetry.Attributes.PayloadWarningThresholdExceeded)));
        Assert.True(Assert.IsType<bool>(
            offloadedPayload.Tag(ServiceBusTelemetry.Attributes.PayloadWarningThresholdExceeded)));
        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.PayloadAdmission,
            ServiceBusTelemetry.Attributes.ErrorType,
            "payload-body-too-large",
            "message-data-required",
            "transport-envelope-too-large",
            "payload-admission");
        AssertTagValues(
            observations,
            ServiceBusTelemetry.Metrics.PayloadEnvelopeSize,
            ServiceBusTelemetry.Attributes.Outcome,
            "accepted",
            "rejected");

        AssertGauge(observations, ServiceBusTelemetry.Metrics.DurableSenderStored, 11);
        AssertGauge(observations, ServiceBusTelemetry.Metrics.DurableSenderStoredContentSize, 12);
        AssertGauge(observations, ServiceBusTelemetry.Metrics.DurableSenderPending, 13);
        AssertGauge(observations, ServiceBusTelemetry.Metrics.DurableSenderRetryScheduled, 14);
        AssertGauge(observations, ServiceBusTelemetry.Metrics.DurableSenderAwaitingConsumerCompletion, 15);
        AssertGauge(observations, ServiceBusTelemetry.Metrics.DurableSenderQuarantined, 16);
        AssertGauge(observations, ServiceBusTelemetry.Metrics.DurableSenderOldestPendingAge, 17);

        instrumentation.PublishDurableSnapshot(
            new DurableSendStoreSnapshot(0, 0, 0, 0, 0, 0, Epoch.AddSeconds(20)),
            Epoch);
        observations.RecordObservableInstruments();
        Assert.Equal(0, observations.Measurements
            .Last(measurement => measurement.Name == ServiceBusTelemetry.Metrics.DurableSenderOldestPendingAge)
            .Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-INSTRUMENTS", "durable-activities-exact-bounded-tags")]
    public void DurableActivities_ExposeTheExactIdentityAndDeliveryAttemptWithoutPayloadData()
    {
        var stopped = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Enqueue,
        };
        ActivitySource.AddActivityListener(listener);
        using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        using var instrumentation = new ServiceBusInstrumentation<IBus>(provider.GetRequiredService<IMeterFactory>());
        SerializedDurableSend message = CreateMessage();
        var delivery = new DurableSendDelivery
        {
            Message = message,
            GenerationToken = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            EnqueuedAt = Epoch,
            DeliveryAttempts = 2,
            Status = DurableSendStatus.Pending,
            Lease = new DurableSendLease(Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa"), Epoch.AddMinutes(1)),
        };

        using (SafeActivityScope admission = instrumentation.StartDurableAdmission(message))
            admission.SetFailure("tests.expected");
        using (SafeActivityScope deliveryActivity = instrumentation.StartDurableDelivery(delivery))
            deliveryActivity.SetTag("tests.result", "observed");

        Activity[] activities = stopped.ToArray();
        Assert.Equal(["durable send admit", "durable send deliver"], activities.Select(activity => activity.OperationName));
        Activity admissionActivity = activities[0];
        Assert.Equal(ActivityStatusCode.Error, admissionActivity.Status);
        Assert.Equal("tests.expected", admissionActivity.GetTagItem(ServiceBusTelemetry.Attributes.ErrorType));
        Assert.Equal("durable-admit", admissionActivity.GetTagItem(ServiceBusTelemetry.Attributes.OperationName));
        Assert.Equal(message.Id.ToString(), admissionActivity.GetTagItem(ServiceBusTelemetry.Attributes.DurableSendId));
        Assert.Equal(message.MessageId?.ToString("D"), admissionActivity.GetTagItem(ServiceBusTelemetry.Attributes.MessageId));
        Assert.Equal(message.ContractIdentity.ToString(), admissionActivity.GetTagItem(ServiceBusTelemetry.Attributes.MessageContract));
        Assert.Equal(message.StorageSize, admissionActivity.GetTagItem(ServiceBusTelemetry.Attributes.DurableSenderRetainedContentSize));
        Assert.DoesNotContain(admissionActivity.TagObjects, tag => tag.Value is ReadOnlyMemory<byte> or byte[]);

        Activity deliveredActivity = activities[1];
        Assert.Equal("durable-deliver", deliveredActivity.GetTagItem(ServiceBusTelemetry.Attributes.OperationName));
        Assert.Equal(3, deliveredActivity.GetTagItem(ServiceBusTelemetry.Attributes.DeliveryAttempt));
        Assert.Equal("observed", deliveredActivity.GetTagItem("tests.result"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-INSTRUMENTS", "broken-or-disposed-instrumentation-is-inert")]
    public void BrokenOrDisposedInstrumentation_CannotChangeMessagingBehavior()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceBusInstrumentation<IBus>(null!));
        Assert.Throws<ArgumentNullException>(() => new SafeActivityScope(null!));

        var throwingMeterFactory = new ThrowingMeterFactory();
        using (var broken = new ServiceBusInstrumentation<IBus>(throwingMeterFactory))
        {
            broken.RecordDurableAdmission(DurableSendAdmissionDisposition.Accepted, 1);
            broken.RecordDurableDelivery(DurableSendDeliveryOutcome.Delivered, failureKind: null, 0.1);
            broken.RecordPayloadBody(PayloadAdmissionDisposition.Inline, 1, warningThresholdExceeded: false);
        }
        Assert.Equal(1, throwingMeterFactory.CreateCallCount);

        var sampleCallCount = 0;
        using (var samplingListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
            {
                Interlocked.Increment(ref sampleCallCount);
                throw new HostileTelemetryException();
            },
        })
        {
            ActivitySource.AddActivityListener(samplingListener);
            using var samplingBoundary = new ServiceBusInstrumentation<IBus>(throwingMeterFactory);
            using SafeActivityScope ignored = samplingBoundary.StartDurableAdmission(CreateMessage());
        }
        Assert.Equal(1, sampleCallCount);

        var stopCallCount = 0;
        using (var stoppingListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _ =>
            {
                Interlocked.Increment(ref stopCallCount);
                throw new HostileTelemetryException();
            },
        })
        {
            ActivitySource.AddActivityListener(stoppingListener);
            using var stoppingBoundary = new ServiceBusInstrumentation<IBus>(throwingMeterFactory);
            SafeActivityScope scope = stoppingBoundary.StartDurableAdmission(CreateMessage());
            scope.Dispose();
        }
        Assert.Equal(1, stopCallCount);

        using ServiceProvider provider = new ServiceCollection().AddMetrics().BuildServiceProvider();
        IMeterFactory meterFactory = provider.GetRequiredService<IMeterFactory>();
        using var observations = new MetricObservationSession(meterFactory);
        var disposed = new ServiceBusInstrumentation<IBus>(meterFactory);
        disposed.RecordPayloadEnvelope(1, rejected: false);
        int beforeDispose = observations.Measurements.Count;
        disposed.Dispose();

        disposed.RecordPayloadEnvelope(2, rejected: true);
        disposed.RecordDurableAdmission(DurableSendAdmissionDisposition.Accepted, 2);
        using SafeActivityScope activity = disposed.StartDurableAdmission(CreateMessage());
        activity.SetFailure("ignored");

        Assert.Equal(beforeDispose, observations.Measurements.Count);
    }

    private static SerializedDurableSend CreateMessage() => new()
    {
        Id = new DurableSendId(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")),
        ContractIdentity = new MessageContractIdentity("vicione.tests.telemetry", 1),
        DestinationAddress = new Uri("loopback://telemetry/messages"),
        ContentType = "application/json",
        Body = new byte[] { 1, 2, 3 },
        Metadata = new byte[] { 4, 5 },
        MessageId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"),
    };

    private static void AssertInstrument<TInstrument>(
        MetricObservationSession observations,
        string name,
        string? unit)
        where TInstrument : Instrument
    {
        MetricInstrumentDescriptor instrument = Assert.Single(observations.Instruments, item => item.Name == name);
        Assert.Equal(typeof(TInstrument), instrument.InstrumentType);
        Assert.Equal(unit, instrument.Unit);
    }

    private static void AssertTagValues(
        MetricObservationSession observations,
        string instrumentName,
        string attributeName,
        params string[] expected)
    {
        string[] actual = observations.Measurements
            .Where(measurement => measurement.Name == instrumentName)
            .SelectMany(measurement => measurement.Tags)
            .Where(tag => tag.Key == attributeName)
            .Select(tag => Assert.IsType<string>(tag.Value))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
    }

    private static void AssertGauge(MetricObservationSession observations, string name, double expected)
        => Assert.Equal(expected, Assert.Single(observations.Measurements, measurement => measurement.Name == name).Value);

    private sealed class ThrowingMeterFactory : IMeterFactory
    {
        public int CreateCallCount { get; private set; }

        public Meter Create(MeterOptions options)
        {
            CreateCallCount++;
            throw new HostileTelemetryException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class HostileTelemetryException : Exception;
}
