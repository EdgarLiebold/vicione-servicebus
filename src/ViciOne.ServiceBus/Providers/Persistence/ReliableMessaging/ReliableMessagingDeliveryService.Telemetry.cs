using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Monitoring.Telemetry;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class ReliableMessagingDeliveryService<TBus>
    where TBus : class, IBus
{
    void RecordOutcome(
        SafeActivityScope activity,
        DurableSendDeliveryOutcome outcome,
        DurableSendFailureKind? failureKind,
        long? started)
    {
        activity.SetTag(ServiceBusTelemetry.Attributes.Outcome, OutcomeTag(outcome));
        if (failureKind is not null)
            activity.SetFailure(ServiceBusInstrumentation<TBus>.ErrorType(failureKind.Value));
        _instrumentation.RecordDurableDelivery(
            outcome,
            failureKind,
            TryGetElapsedSeconds(started));
    }

    void RecordCanceled(SafeActivityScope activity, long? started)
        => RecordOutcome(activity, DurableSendDeliveryOutcome.Canceled, failureKind: null, started);

    void RecordStatePersistenceFailure(
        DurableSendId id,
        string dispatchOutcome,
        Exception persistenceException,
        SafeActivityScope activity,
        long? started)
    {
        activity.SetTag(ServiceBusTelemetry.Attributes.Outcome, "state-persistence-failed");
        activity.SetFailure("durable-state-persistence-failure");
        _instrumentation.RecordDurableDelivery(
            DurableSendDeliveryOutcome.StatePersistenceFailed,
            failureKind: null,
            TryGetElapsedSeconds(started));
        TryLogStatePersistenceFailed(
            id.Value,
            dispatchOutcome,
            DiagnosticTypeName(persistenceException.GetType()));
    }

    double? TryGetElapsedSeconds(long? started)
    {
        if (started is not { } timestamp)
            return null;

        try
        {
            return _timeProvider.GetElapsedTime(timestamp).TotalSeconds;
        }
        catch
        {
            // An unavailable duration cannot replace the actual persisted, failed or canceled outcome.
            return null;
        }
    }

    async Task RefreshTelemetrySnapshotIfDueAsync(DateTimeOffset claimTime, CancellationToken cancellationToken)
    {
        long next = Volatile.Read(ref _nextTelemetrySnapshotUtcTicks);
        if (Volatile.Read(ref _telemetrySnapshotHasNoSuccessor) != 0 || claimTime.UtcTicks < next)
            return;

        try
        {
            // Reserve from the required claim time before any optional clock or storage read can fail.
            ReserveTelemetrySnapshotSuccessor(claimTime);
            DateTimeOffset now = _timeProvider.GetUtcNow();
            // Delivery may be slow. Successful observations start the minimum interval at their actual UTC.
            ReserveTelemetrySnapshotSuccessor(now);
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

    void ReserveTelemetrySnapshotSuccessor(DateTimeOffset basis)
    {
        long intervalTicks = RequirePolicy().TelemetrySnapshotInterval.Ticks;
        if (intervalTicks > DateTimeOffset.MaxValue.UtcTicks - basis.UtcTicks)
        {
            // There is no later representable UTC. Allow this attempt, then stop repeated preparation at MaxValue.
            Volatile.Write(ref _telemetrySnapshotHasNoSuccessor, 1);
            return;
        }

        long successor = basis.UtcTicks + intervalTicks;
        if (successor > Volatile.Read(ref _nextTelemetrySnapshotUtcTicks))
            Volatile.Write(ref _nextTelemetrySnapshotUtcTicks, successor);
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
