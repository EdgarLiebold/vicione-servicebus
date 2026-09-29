using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Threading;
using ViciOne.ServiceBus.Logging.Diagnostics;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Monitoring.Telemetry;

/// <summary>
/// Records exception-isolated messaging telemetry for one typed bus. The host owns listeners,
/// exporters, sampling, retention, access control and telemetry endpoints.
/// </summary>
/// <typeparam name="TBus">The bus type that scopes metric dimensions.</typeparam>
internal sealed class ServiceBusInstrumentation<TBus> : IDisposable
    where TBus : class
{
    readonly ActivitySource _activitySource;
    readonly object _metricInitializationLock = new();
    readonly Lazy<Meter?> _meter;
    Counter<long>? _durableAdmission;
    Histogram<long>? _durableAdmissionStorageSize;
    Counter<long>? _durableDelivery;
    Histogram<double>? _durableDeliveryDuration;
    Counter<long>? _durableConsumerCompletion;
    Histogram<double>? _durableConsumerCompletionDuration;
    Counter<long>? _reliabilityAbandoned;
    Counter<long>? _payloadAdmission;
    Histogram<long>? _payloadBodySize;
    Histogram<long>? _payloadEnvelopeSize;
    long _durableStoredCount;
    long _durableStoredBytes;
    long _durablePendingCount;
    long _durableRetryScheduledCount;
    long _durableAwaitingConsumerCompletionCount;
    long _durableQuarantinedCount;
    long _durableOldestPendingAgeSeconds;
    int _durableMetricsInitialized;
    int _payloadMetricsInitialized;
    int _disposed;

    public ServiceBusInstrumentation(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        string? version = typeof(ServiceBusInstrumentation<>).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        KeyValuePair<string, object?>[] meterTags =
        [
            new(ServiceBusTelemetry.Attributes.Bus, typeof(TBus).FullName ?? typeof(TBus).Name),
        ];

        _activitySource = new ActivitySource(ServiceBusTelemetry.ActivitySourceName, version);
        _meter = new Lazy<Meter?>(() => TryCreateMeter(meterFactory, version, meterTags), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SafeActivityScope StartDurableAdmission(SerializedDurableSend message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (Volatile.Read(ref _disposed) != 0)
            return SafeActivityScope.None;

        try
        {
            Activity? activity = ActivityObservation.TryStartSource(
                _activitySource, "durable send admit", ActivityKind.Internal);
            if (activity is null)
                return SafeActivityScope.None;

            activity.SetTag(ServiceBusTelemetry.Attributes.Bus, typeof(TBus).FullName ?? typeof(TBus).Name);
            activity.SetTag(ServiceBusTelemetry.Attributes.OperationName, "durable-admit");
            activity.SetTag(ServiceBusTelemetry.Attributes.OperationType, "create");
            activity.SetTag(ServiceBusTelemetry.Attributes.DestinationName, message.DestinationAddress.ToString());
            activity.SetTag(ServiceBusTelemetry.Attributes.DurableSendId, message.Id.ToString());
            if (message.MessageId is { } messageId)
                activity.SetTag(ServiceBusTelemetry.Attributes.MessageId, messageId.ToString("D"));
            activity.SetTag(ServiceBusTelemetry.Attributes.MessageContract, message.ContractIdentity.ToString());
            activity.SetTag(ServiceBusTelemetry.Attributes.DurableSenderRetainedContentSize, message.StorageSize);
            return new SafeActivityScope(activity);
        }
        catch
        {
            return SafeActivityScope.None;
        }
    }

    public SafeActivityScope StartDurableDelivery(DurableSendDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (Volatile.Read(ref _disposed) != 0)
            return SafeActivityScope.None;

        try
        {
            Activity? activity = ActivityObservation.TryStartSource(
                _activitySource, "durable send deliver", ActivityKind.Internal);
            if (activity is null)
                return SafeActivityScope.None;

            activity.SetTag(ServiceBusTelemetry.Attributes.Bus, typeof(TBus).FullName ?? typeof(TBus).Name);
            activity.SetTag(ServiceBusTelemetry.Attributes.OperationName, "durable-deliver");
            activity.SetTag(ServiceBusTelemetry.Attributes.OperationType, "send");
            activity.SetTag(ServiceBusTelemetry.Attributes.DestinationName, delivery.Message.DestinationAddress.ToString());
            activity.SetTag(ServiceBusTelemetry.Attributes.DurableSendId, delivery.Message.Id.ToString());
            if (delivery.Message.MessageId is { } messageId)
                activity.SetTag(ServiceBusTelemetry.Attributes.MessageId, messageId.ToString("D"));
            activity.SetTag(ServiceBusTelemetry.Attributes.MessageContract, delivery.Message.ContractIdentity.ToString());
            activity.SetTag(ServiceBusTelemetry.Attributes.DeliveryAttempt, delivery.DeliveryAttempts + 1);
            return new SafeActivityScope(activity);
        }
        catch
        {
            return SafeActivityScope.None;
        }
    }

    public void RecordDurableAdmission(DurableSendAdmissionDisposition disposition, long storageSize)
        => RecordDurableAdmissionCore(AdmissionOutcome(disposition), errorType: null, storageSize);

    public void RecordDurableAdmissionRejected(DurableSendAdmissionFailure failure, long storageSize)
        => RecordDurableAdmissionCore("rejected", AdmissionErrorType(failure), storageSize);

    void RecordDurableAdmissionCore(string outcome, string? errorType, long storageSize)
    {
        try
        {
            if (!EnsureDurableMetrics())
                return;

            TagList tags = new()
            {
                { ServiceBusTelemetry.Attributes.Outcome, outcome },
            };
            if (errorType is not null)
                tags.Add(ServiceBusTelemetry.Attributes.ErrorType, errorType);

            _durableAdmission?.Add(1, in tags);
            _durableAdmissionStorageSize?.Record(storageSize, in tags);
        }
        catch
        {
            // Observation is never allowed to rewrite durable acceptance semantics.
        }
    }

    public void RecordDurableDelivery(
        DurableSendDeliveryOutcome outcome,
        DurableSendFailureKind? failureKind,
        double elapsedSeconds)
    {
        try
        {
            if (!EnsureDurableMetrics())
                return;

            TagList tags = new()
            {
                { ServiceBusTelemetry.Attributes.Outcome, DeliveryOutcome(outcome) },
            };
            if (failureKind is not null)
                tags.Add(ServiceBusTelemetry.Attributes.ErrorType, ErrorType(failureKind.Value));
            else if (outcome == DurableSendDeliveryOutcome.StatePersistenceFailed)
                tags.Add(ServiceBusTelemetry.Attributes.ErrorType, "durable-state-persistence-failure");

            _durableDelivery?.Add(1, in tags);
            _durableDeliveryDuration?.Record(elapsedSeconds, in tags);
        }
        catch
        {
            // A hostile listener/exporter cannot change delivery state.
        }
    }

    public void RecordDurableConsumerCompletion(bool retired, double elapsedSeconds)
    {
        try
        {
            if (!EnsureDurableMetrics())
                return;

            TagList tags = new()
            {
                { ServiceBusTelemetry.Attributes.Outcome, retired ? "retired" : "not-retired" },
            };
            _durableConsumerCompletion?.Add(1, in tags);
            _durableConsumerCompletionDuration?.Record(elapsedSeconds, in tags);
        }
        catch
        {
            // Completion is a correctness signal; observation cannot affect it.
        }
    }

    public void RecordReliabilityAbandoned(ReliableMessageKind kind)
    {
        try
        {
            if (!EnsureDurableMetrics())
                return;

            TagList tags = new()
            {
                { ServiceBusTelemetry.Attributes.ReliabilitySide, kind == ReliableMessageKind.Inbox ? "inbox" : "outbox" },
            };
            _reliabilityAbandoned?.Add(1, in tags);
        }
        catch
        {
            // Operator state is authoritative; an observation failure cannot rewrite it.
        }
    }

    public void RecordPayloadBody(PayloadAdmissionDisposition disposition, int bytes, bool warningThresholdExceeded)
    {
        try
        {
            if (!EnsurePayloadMetrics())
                return;

            TagList tags = new()
            {
                { ServiceBusTelemetry.Attributes.Outcome, disposition == PayloadAdmissionDisposition.Inline ? "inline" : "message-data" },
                { ServiceBusTelemetry.Attributes.PayloadWarningThresholdExceeded, warningThresholdExceeded },
            };
            _payloadBodySize?.Record(bytes, in tags);
            _payloadAdmission?.Add(1, in tags);
        }
        catch
        {
            // Payload admission correctness cannot depend on metrics listeners.
        }
    }

    public void RecordPayloadRejected(PayloadAdmissionStage stage, long bytes)
    {
        try
        {
            if (!EnsurePayloadMetrics())
                return;

            TagList tags = new()
            {
                { ServiceBusTelemetry.Attributes.Outcome, "rejected" },
                { ServiceBusTelemetry.Attributes.ErrorType, PayloadErrorType(stage) },
            };
            if (stage == PayloadAdmissionStage.TransportEnvelope)
                _payloadEnvelopeSize?.Record(bytes, in tags);
            else
                _payloadBodySize?.Record(bytes, in tags);
            _payloadAdmission?.Add(1, in tags);
        }
        catch
        {
            // Metric listeners cannot change a rejected payload outcome.
        }
    }

    public void RecordPayloadEnvelope(int bytes, bool rejected)
    {
        try
        {
            if (!EnsurePayloadMetrics())
                return;

            TagList tags = new()
            {
                { ServiceBusTelemetry.Attributes.Outcome, rejected ? "rejected" : "accepted" },
            };
            _payloadEnvelopeSize?.Record(bytes, in tags);
        }
        catch
        {
            // Metric listeners cannot change transport-envelope admission.
        }
    }

    public void PublishDurableSnapshot(DurableSendStoreSnapshot snapshot, DateTimeOffset observedAt)
    {
        if (!EnsureDurableMetrics())
            return;

        // Observable callbacks read these atomics only; they never call storage or block.
        Volatile.Write(ref _durableStoredCount, snapshot.StoredCount);
        Volatile.Write(ref _durableStoredBytes, snapshot.StoredBytes);
        Volatile.Write(ref _durablePendingCount, snapshot.PendingCount);
        Volatile.Write(ref _durableRetryScheduledCount, snapshot.RetryScheduledCount);
        Volatile.Write(ref _durableAwaitingConsumerCompletionCount, snapshot.AwaitingConsumerCompletionCount);
        Volatile.Write(ref _durableQuarantinedCount, snapshot.QuarantinedCount);

        long age = snapshot.OldestPendingEnqueuedAt is { } oldest
            ? Math.Max(0, (long)(observedAt - oldest).TotalSeconds)
            : 0;
        Volatile.Write(ref _durableOldestPendingAgeSeconds, age);
    }

    bool EnsurePayloadMetrics()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return false;

        if (Volatile.Read(ref _payloadMetricsInitialized) != 0)
            return _payloadAdmission is not null;

        lock (_metricInitializationLock)
        {
            if (_payloadMetricsInitialized != 0)
                return _payloadAdmission is not null;

            try
            {
                Meter? meter = _meter.Value;
                if (meter is null)
                    return false;

                var admission = meter.CreateCounter<long>(
                    ServiceBusTelemetry.Metrics.PayloadAdmission,
                    unit: "{decision}",
                    description: "Serialized payload admission decisions.");
                var bodySize = meter.CreateHistogram<long>(
                    ServiceBusTelemetry.Metrics.PayloadBodySize,
                    unit: "By",
                    description: "Exact serialized application-body size evaluated by admission policy.");
                var envelopeSize = meter.CreateHistogram<long>(
                    ServiceBusTelemetry.Metrics.PayloadEnvelopeSize,
                    unit: "By",
                    description: "Exact final transport-envelope size evaluated by admission policy.");

                _payloadAdmission = admission;
                _payloadBodySize = bodySize;
                _payloadEnvelopeSize = envelopeSize;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                Volatile.Write(ref _payloadMetricsInitialized, 1);
            }
        }
    }

    bool EnsureDurableMetrics()
    {
        if (Volatile.Read(ref _disposed) != 0)
            return false;

        if (Volatile.Read(ref _durableMetricsInitialized) != 0)
            return _durableAdmission is not null;

        lock (_metricInitializationLock)
        {
            if (_durableMetricsInitialized != 0)
                return _durableAdmission is not null;

            try
            {
                Meter? meter = _meter.Value;
                if (meter is null)
                    return false;

                var admission = meter.CreateCounter<long>(
                    ServiceBusTelemetry.Metrics.DurableSenderAdmission,
                    unit: "{request}",
                    description: "Durable sender admission outcomes.");
                var admissionStorageSize = meter.CreateHistogram<long>(
                    ServiceBusTelemetry.Metrics.DurableSenderAdmissionSize,
                    unit: "By",
                    description: "Logical retained content bytes of one durable-send admission request (serialized body + ServiceBus metadata).");
                var delivery = meter.CreateCounter<long>(
                    ServiceBusTelemetry.Metrics.DurableSenderDelivery,
                    unit: "{attempt}",
                    description: "Durable sender delivery-attempt outcomes.");
                var deliveryDuration = meter.CreateHistogram<double>(
                    ServiceBusTelemetry.Metrics.DurableSenderDeliveryDuration,
                    unit: "s",
                    description: "Duration of one durable sender delivery attempt.");
                var consumerCompletion = meter.CreateCounter<long>(
                    ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletion,
                    unit: "{completion}",
                    description: "Process-local consumer-completion outcomes for volatile durable sends.");
                var consumerCompletionDuration = meter.CreateHistogram<double>(
                    ServiceBusTelemetry.Metrics.DurableSenderConsumerCompletionDuration,
                    unit: "s",
                    description: "Elapsed time from volatile durable dispatch attempt start to logical consumer completion.");
                var reliabilityAbandoned = meter.CreateCounter<long>(
                    ServiceBusTelemetry.Metrics.ReliabilityAbandoned,
                    unit: "{decision}",
                    description: "Explicit operator decisions to retain a quarantined reliable-messaging record as abandoned.");

                meter.CreateObservableGauge(
                    ServiceBusTelemetry.Metrics.DurableSenderStored,
                    () => Volatile.Read(ref _durableStoredCount),
                    unit: "{message}",
                    description: "Last observed retained durable-send record count.");
                meter.CreateObservableGauge(
                    ServiceBusTelemetry.Metrics.DurableSenderStoredContentSize,
                    () => Volatile.Read(ref _durableStoredBytes),
                    unit: "By",
                    description: "Last observed logical retained durable-send content bytes (serialized body + ServiceBus metadata).");
                meter.CreateObservableGauge(
                    ServiceBusTelemetry.Metrics.DurableSenderPending,
                    () => Volatile.Read(ref _durablePendingCount),
                    unit: "{message}",
                    description: "Last observed durable-send pending count.");
                meter.CreateObservableGauge(
                    ServiceBusTelemetry.Metrics.DurableSenderRetryScheduled,
                    () => Volatile.Read(ref _durableRetryScheduledCount),
                    unit: "{message}",
                    description: "Last observed durable-send retry-scheduled count.");
                meter.CreateObservableGauge(
                    ServiceBusTelemetry.Metrics.DurableSenderAwaitingConsumerCompletion,
                    () => Volatile.Read(ref _durableAwaitingConsumerCompletionCount),
                    unit: "{message}",
                    description: "Last observed durable-send count awaiting logical consumer completion.");
                meter.CreateObservableGauge(
                    ServiceBusTelemetry.Metrics.DurableSenderQuarantined,
                    () => Volatile.Read(ref _durableQuarantinedCount),
                    unit: "{message}",
                    description: "Last observed durable-send quarantine count.");
                meter.CreateObservableGauge(
                    ServiceBusTelemetry.Metrics.DurableSenderOldestPendingAge,
                    () => Volatile.Read(ref _durableOldestPendingAgeSeconds),
                    unit: "s",
                    description: "Age of the oldest pending durable-send intent at the last store snapshot.");

                _durableAdmission = admission;
                _durableAdmissionStorageSize = admissionStorageSize;
                _durableDelivery = delivery;
                _durableDeliveryDuration = deliveryDuration;
                _durableConsumerCompletion = consumerCompletion;
                _durableConsumerCompletionDuration = consumerCompletionDuration;
                _reliabilityAbandoned = reliabilityAbandoned;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                Volatile.Write(ref _durableMetricsInitialized, 1);
            }
        }
    }

    static Meter? TryCreateMeter(
        IMeterFactory meterFactory,
        string? version,
        IEnumerable<KeyValuePair<string, object?>> meterTags)
    {
        try
        {
            return meterFactory.Create(ServiceBusTelemetry.MeterName, version, meterTags);
        }
        catch
        {
            // A host-owned meter factory is an observation boundary. A broken factory leaves this
            // typed instrumentation instance inert and must never rewrite a messaging outcome.
            return null;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            _activitySource.Dispose();
        }
        catch
        {
            // Instrumentation disposal cannot fail service shutdown.
        }
    }

    internal static string ErrorType(DurableSendFailureKind failureKind)
        => failureKind switch
        {
            DurableSendFailureKind.None => "none",
            DurableSendFailureKind.Transient => "transient",
            DurableSendFailureKind.NonRetryable => "non-retryable",
            DurableSendFailureKind.Unclassified => "unclassified",
            DurableSendFailureKind.RetryLimitExceeded => "retry-limit-exceeded",
            DurableSendFailureKind.InvariantViolation => "invariant-violation",
            DurableSendFailureKind.ConsumerCompletionTimeout => "consumer-completion-timeout",
            _ => "unknown",
        };

    static string AdmissionOutcome(DurableSendAdmissionDisposition disposition)
        => disposition switch
        {
            DurableSendAdmissionDisposition.Accepted => "accepted",
            DurableSendAdmissionDisposition.AlreadyAccepted => "already-accepted",
            DurableSendAdmissionDisposition.AlreadyQuarantined => "already-quarantined",
            _ => "unknown",
        };

    static string DeliveryOutcome(DurableSendDeliveryOutcome outcome)
        => outcome switch
        {
            DurableSendDeliveryOutcome.Delivered => "delivered",
            DurableSendDeliveryOutcome.AwaitingConsumerCompletion => "awaiting-consumer-completion",
            DurableSendDeliveryOutcome.RetryScheduled => "retry-scheduled",
            DurableSendDeliveryOutcome.Quarantined => "quarantined",
            DurableSendDeliveryOutcome.Canceled => "canceled",
            DurableSendDeliveryOutcome.StatePersistenceFailed => "state-persistence-failed",
            _ => "unknown",
        };

    static string AdmissionErrorType(DurableSendAdmissionFailure failure)
        => failure switch
        {
            DurableSendAdmissionFailure.CapacityExceeded => "capacity-exceeded",
            DurableSendAdmissionFailure.IdentityConflict => "identity-conflict",
            DurableSendAdmissionFailure.ContractNotRegistered => "contract-not-registered",
            DurableSendAdmissionFailure.StoreFailure => "durable-store-failure",
            _ => "admission-failed",
        };

    static string PayloadErrorType(PayloadAdmissionStage stage)
        => stage switch
        {
            PayloadAdmissionStage.SerializedBody => "payload-body-too-large",
            PayloadAdmissionStage.MessageData => "message-data-required",
            PayloadAdmissionStage.TransportEnvelope => "transport-envelope-too-large",
            _ => "payload-admission",
        };
}
