
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Threading;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Diagnostics;
/// <summary>
/// DI-owned V5 instrumentation for one typed bus. The host owns listeners, exporters, sampling,
/// retention, RBAC and telemetry endpoints. Every observation path is exception-isolated so that
/// telemetry can disappear but can never rewrite a messaging outcome.
/// </summary>
internal sealed class V5ServiceBusInstrumentation<TBus> : IDisposable
    where TBus : class
{
    internal const string InstrumentationName = "ViciOne.ServiceBus";

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

    public V5ServiceBusInstrumentation(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        string? version = typeof(V5ServiceBusInstrumentation<>).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        KeyValuePair<string, object?>[] meterTags =
        [
            new("vicione.servicebus.bus", typeof(TBus).FullName ?? typeof(TBus).Name),
        ];

        _activitySource = new ActivitySource(InstrumentationName, version);
        _meter = new Lazy<Meter?>(() => TryCreateMeter(meterFactory, version, meterTags), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SafeActivityScope StartDurableAdmission(SerializedDurableSend message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (Volatile.Read(ref _disposed) != 0)
            return default;

        try
        {
            Activity? activity = _activitySource.StartActivity("durable send admit", ActivityKind.Internal);
            if (activity is null)
                return default;

            activity.SetTag("vicione.servicebus.bus", typeof(TBus).FullName ?? typeof(TBus).Name);
            activity.SetTag("vicione.servicebus.operation", "durable-admit");
            activity.SetTag("vicione.servicebus.durable_send.id", message.Id.ToString());
            if (message.MessageId is { } messageId)
                activity.SetTag("messaging.message.id", messageId.ToString("D"));
            activity.SetTag("vicione.servicebus.contract", message.ContractIdentity.ToString());
            activity.SetTag("vicione.servicebus.durable_sender.retained_content.size", message.StorageSize);
            return new SafeActivityScope(activity);
        }
        catch
        {
            return default;
        }
    }

    public SafeActivityScope StartDurableDelivery(DurableSendDelivery delivery)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (Volatile.Read(ref _disposed) != 0)
            return default;

        try
        {
            Activity? activity = _activitySource.StartActivity("durable send deliver", ActivityKind.Internal);
            if (activity is null)
                return default;

            activity.SetTag("vicione.servicebus.bus", typeof(TBus).FullName ?? typeof(TBus).Name);
            activity.SetTag("vicione.servicebus.operation", "durable-deliver");
            activity.SetTag("vicione.servicebus.durable_send.id", delivery.Message.Id.ToString());
            if (delivery.Message.MessageId is { } messageId)
                activity.SetTag("messaging.message.id", messageId.ToString("D"));
            activity.SetTag("vicione.servicebus.contract", delivery.Message.ContractIdentity.ToString());
            activity.SetTag("vicione.servicebus.delivery.attempt", delivery.DeliveryAttempts + 1);
            return new SafeActivityScope(activity);
        }
        catch
        {
            return default;
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
                { "outcome", outcome },
            };
            if (errorType is not null)
                tags.Add("error.type", errorType);

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
                { "outcome", DeliveryOutcome(outcome) },
            };
            if (failureKind is not null)
                tags.Add("error.type", ErrorType(failureKind.Value));
            else if (outcome == DurableSendDeliveryOutcome.StatePersistenceFailed)
                tags.Add("error.type", "durable-state-persistence-failure");

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
                { "outcome", retired ? "retired" : "not-retired" },
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
                { "side", kind == ReliableMessageKind.Inbox ? "inbox" : "outbox" },
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
                { "outcome", disposition == PayloadAdmissionDisposition.Inline ? "inline" : "message-data" },
                { "warning", warningThresholdExceeded ? "true" : "false" },
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
                { "outcome", "rejected" },
                { "error.type", PayloadErrorType(stage) },
            };
            if (stage == PayloadAdmissionStage.TransportEnvelope)
                _payloadEnvelopeSize?.Record(bytes, in tags);
            else
                _payloadBodySize?.Record(bytes, in tags);
            _payloadAdmission?.Add(1, in tags);
        }
        catch
        {
            // Observation only.
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
                { "outcome", rejected ? "rejected" : "accepted" },
            };
            _payloadEnvelopeSize?.Record(bytes, in tags);
        }
        catch
        {
            // Observation only.
        }
    }

    public void PublishDurableSnapshot(DurableSendStoreSnapshot snapshot, DateTimeOffset observedAt)
    {
        EnsureDurableMetrics();

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
                    "vicione.servicebus.payload.admission",
                    description: "Serialized payload admission decisions.");
                var bodySize = meter.CreateHistogram<long>(
                    "vicione.servicebus.payload.body.size",
                    unit: "By",
                    description: "Exact serialized application-body size evaluated by admission policy.");
                var envelopeSize = meter.CreateHistogram<long>(
                    "vicione.servicebus.payload.envelope.size",
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
                    "vicione.servicebus.durable_sender.admission",
                    description: "Durable sender admission outcomes.");
                var admissionStorageSize = meter.CreateHistogram<long>(
                    "vicione.servicebus.durable_sender.admission.size",
                    unit: "By",
                    description: "Logical retained content bytes of one durable-send admission request (serialized body + ServiceBus metadata).");
                var delivery = meter.CreateCounter<long>(
                    "vicione.servicebus.durable_sender.delivery",
                    description: "Durable sender delivery-attempt outcomes.");
                var deliveryDuration = meter.CreateHistogram<double>(
                    "vicione.servicebus.durable_sender.delivery.duration",
                    unit: "s",
                    description: "Duration of one durable sender delivery attempt.");
                var consumerCompletion = meter.CreateCounter<long>(
                    "vicione.servicebus.durable_sender.consumer_completion",
                    description: "Process-local consumer-completion outcomes for volatile durable sends.");
                var consumerCompletionDuration = meter.CreateHistogram<double>(
                    "vicione.servicebus.durable_sender.consumer_completion.duration",
                    unit: "s",
                    description: "Elapsed time from volatile durable dispatch attempt start to logical consumer completion.");
                var reliabilityAbandoned = meter.CreateCounter<long>(
                    "vicione.servicebus.reliability.abandoned",
                    description: "Explicit operator decisions to retain a quarantined reliable-messaging record as abandoned.");

                meter.CreateObservableGauge(
                    "vicione.servicebus.durable_sender.stored",
                    () => Volatile.Read(ref _durableStoredCount),
                    description: "Last observed retained durable-send record count.");
                meter.CreateObservableGauge(
                    "vicione.servicebus.durable_sender.stored.content.size",
                    () => Volatile.Read(ref _durableStoredBytes),
                    unit: "By",
                    description: "Last observed logical retained durable-send content bytes (serialized body + ServiceBus metadata).");
                meter.CreateObservableGauge(
                    "vicione.servicebus.durable_sender.pending",
                    () => Volatile.Read(ref _durablePendingCount),
                    description: "Last observed durable-send pending count.");
                meter.CreateObservableGauge(
                    "vicione.servicebus.durable_sender.retry_scheduled",
                    () => Volatile.Read(ref _durableRetryScheduledCount),
                    description: "Last observed durable-send retry-scheduled count.");
                meter.CreateObservableGauge(
                    "vicione.servicebus.durable_sender.awaiting_consumer_completion",
                    () => Volatile.Read(ref _durableAwaitingConsumerCompletionCount),
                    description: "Last observed durable-send count awaiting logical consumer completion.");
                meter.CreateObservableGauge(
                    "vicione.servicebus.durable_sender.quarantined",
                    () => Volatile.Read(ref _durableQuarantinedCount),
                    description: "Last observed durable-send quarantine count.");
                meter.CreateObservableGauge(
                    "vicione.servicebus.durable_sender.oldest_pending.age",
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
            return meterFactory.Create(InstrumentationName, version, meterTags);
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
            // Disposal is observation-only.
        }
    }

    internal static string ErrorType(DurableSendFailureKind failureKind)
        => failureKind switch
        {
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
            DurableSendDeliveryOutcome.Cancelled => "cancelled",
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

internal enum DurableSendDeliveryOutcome
{
    /// <summary>
    /// Indicates delivered.
    /// </summary>
    Delivered = 0,
    /// <summary>
    /// Indicates retry scheduled.
    /// </summary>
    RetryScheduled = 1,
    /// <summary>
    /// Indicates quarantined.
    /// </summary>
    Quarantined = 2,
    /// <summary>
    /// Indicates cancelled.
    /// </summary>
    Cancelled = 3,
    /// <summary>
    /// Indicates state persistence failed.
    /// </summary>
    StatePersistenceFailed = 4,
    /// <summary>
    /// Indicates awaiting consumer completion.
    /// </summary>
    AwaitingConsumerCompletion = 5,
}

internal enum DurableSendAdmissionFailure
{
    /// <summary>
    /// Indicates capacity exceeded.
    /// </summary>
    CapacityExceeded = 0,
    /// <summary>
    /// Indicates identity conflict.
    /// </summary>
    IdentityConflict = 1,
    /// <summary>
    /// Indicates contract not registered.
    /// </summary>
    ContractNotRegistered = 2,
    /// <summary>
    /// Indicates store failure.
    /// </summary>
    StoreFailure = 3,
}

/// <summary>Exception-isolating Activity ownership for library instrumentation.</summary>
internal readonly struct SafeActivityScope : IDisposable
{
    readonly Activity? _activity;

    public SafeActivityScope(Activity activity) => _activity = activity;

    public void SetFailure(string errorType)
    {
        if (_activity is null)
            return;

        try
        {
            _activity.SetStatus(ActivityStatusCode.Error);
            _activity.SetTag("error.type", errorType);
        }
        catch
        {
            // Observation only.
        }
    }

    public void SetTag(string name, object? value)
    {
        if (_activity is null)
            return;

        try
        {
            _activity.SetTag(name, value);
        }
        catch
        {
            // Observation only.
        }
    }

    public void Dispose()
    {
        if (_activity is null)
            return;

        try
        {
            _activity.Dispose();
        }
        catch
        {
            // Observation only.
        }
    }
}
