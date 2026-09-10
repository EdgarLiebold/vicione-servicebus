using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Monitoring.Telemetry;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class ReliableMessagingDeliveryService<TBus>
    where TBus : class, IBus
{
    void RecordOutcome(
        SafeActivityScope activity,
        DurableSendDeliveryOutcome outcome,
        DurableSendFailureKind? failureKind,
        long started)
    {
        activity.SetTag("vicione.servicebus.delivery.outcome", OutcomeTag(outcome));
        if (failureKind is not null)
            activity.SetFailure(ServiceBusInstrumentation<TBus>.ErrorType(failureKind.Value));
        _instrumentation.RecordDurableDelivery(
            outcome,
            failureKind,
            _timeProvider.GetElapsedTime(started).TotalSeconds);
    }

    void RecordCanceled(SafeActivityScope activity, long started)
        => RecordOutcome(activity, DurableSendDeliveryOutcome.Canceled, failureKind: null, started);

    void RecordStatePersistenceFailure(
        DurableSendId id,
        string dispatchOutcome,
        Exception persistenceException,
        SafeActivityScope activity,
        long started)
    {
        activity.SetTag("vicione.servicebus.delivery.outcome", "state-persistence-failed");
        activity.SetFailure("durable-state-persistence-failure");
        _instrumentation.RecordDurableDelivery(
            DurableSendDeliveryOutcome.StatePersistenceFailed,
            failureKind: null,
            _timeProvider.GetElapsedTime(started).TotalSeconds);
        TryLogStatePersistenceFailed(
            id.Value,
            dispatchOutcome,
            DiagnosticTypeName(persistenceException.GetType()));
    }

    async Task RefreshTelemetrySnapshotIfDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        long next = Volatile.Read(ref _nextTelemetrySnapshotUtcTicks);
        if (now.UtcTicks < next)
            return;

        // Advance first. If an observation read fails we try again at the configured cadence rather than hot-looping.
        Volatile.Write(ref _nextTelemetrySnapshotUtcTicks, (now + RequirePolicy().TelemetrySnapshotInterval).UtcTicks);

        try
        {
            DurableSendStoreSnapshot snapshot = await _store.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
            _instrumentation.PublishDurableSnapshot(snapshot, now);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The service is stopping. Telemetry refresh never changes shutdown semantics.
        }
        catch (Exception exception)
        {
            TryLogTelemetrySnapshotFailed(DiagnosticTypeName(exception.GetType()));
        }
    }

    static string OutcomeTag(DurableSendDeliveryOutcome outcome)
        => outcome switch
        {
            DurableSendDeliveryOutcome.RetryScheduled => "retry-scheduled",
            DurableSendDeliveryOutcome.Quarantined => "quarantined",
            DurableSendDeliveryOutcome.Delivered => "delivered",
            DurableSendDeliveryOutcome.AwaitingConsumerCompletion => "awaiting-consumer-completion",
            DurableSendDeliveryOutcome.Canceled => "canceled",
            DurableSendDeliveryOutcome.StatePersistenceFailed => "state-persistence-failed",
            _ => "unknown",
        };
}
