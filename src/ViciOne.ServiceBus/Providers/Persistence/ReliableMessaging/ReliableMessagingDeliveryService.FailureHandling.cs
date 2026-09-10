using System;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Monitoring.Telemetry;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class ReliableMessagingDeliveryService<TBus>
    where TBus : class, IBus
{
    async Task PersistTransportFailureAsync(
        DurableSendDelivery delivery,
        Exception dispatchException,
        SafeActivityScope activity,
        long started,
        CancellationToken cancellationToken)
    {
        try
        {
            PersistedFailureOutcome outcome = await PersistFailureAsync(delivery, dispatchException, cancellationToken)
                .ConfigureAwait(false);
            RecordOutcome(activity, outcome.Outcome, outcome.FailureKind, started);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RecordCanceled(activity, started);
            throw;
        }
        catch (Exception persistenceException)
        {
            RecordStatePersistenceFailure(
                delivery.Message.Id,
                $"transport-failure:{dispatchException.GetType().Name}",
                persistenceException,
                activity,
                started);
            throw;
        }
    }

    async Task<PersistedFailureOutcome> PersistFailureAsync(
        DurableSendDelivery delivery,
        Exception exception,
        CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        int attempt = checked(delivery.DeliveryAttempts + 1);
        DurableSendFailureKind failure = Classify(exception);
        string failureType = DiagnosticTypeName(exception.GetType());

        if (failure == DurableSendFailureKind.NonRetryable)
        {
            bool quarantined = await _store.QuarantineAsync(
                    delivery.Message.Id,
                    delivery.Lease,
                    attempt,
                    DurableSendFailureKind.NonRetryable,
                    failureType,
                    now,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!quarantined)
                return new PersistedFailureOutcome(DurableSendDeliveryOutcome.Delivered, FailureKind: null);

            TryLogQuarantinedNonRetryable(
                delivery.Message.Id.Value,
                delivery.Message.ContractIdentity.ToString(),
                failureType);
            return new PersistedFailureOutcome(DurableSendDeliveryOutcome.Quarantined, DurableSendFailureKind.NonRetryable);
        }

        if (failure == DurableSendFailureKind.Transient && attempt < RequirePolicy().MaximumDeliveryAttempts)
        {
            TimeSpan delay = CalculateRetryDelay(delivery.Message.Id, attempt);
            bool scheduled = await _store.ScheduleRetryAsync(
                    delivery.Message.Id,
                    delivery.Lease,
                    attempt,
                    now + delay,
                    DurableSendFailureKind.Transient,
                    failureType,
                    now,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!scheduled)
                return new PersistedFailureOutcome(DurableSendDeliveryOutcome.Delivered, FailureKind: null);

            TryLogRetryScheduled(
                delivery.Message.Id.Value,
                attempt,
                RequirePolicy().MaximumDeliveryAttempts,
                delay.TotalSeconds,
                failureType);
            return new PersistedFailureOutcome(DurableSendDeliveryOutcome.RetryScheduled, DurableSendFailureKind.Transient);
        }

        DurableSendFailureKind terminalKind = failure == DurableSendFailureKind.Transient
            ? DurableSendFailureKind.RetryLimitExceeded
            : DurableSendFailureKind.Unclassified;

        bool terminalQuarantined = await _store.QuarantineAsync(
                delivery.Message.Id,
                delivery.Lease,
                attempt,
                terminalKind,
                failureType,
                now,
                cancellationToken)
            .ConfigureAwait(false);
        if (!terminalQuarantined)
            return new PersistedFailureOutcome(DurableSendDeliveryOutcome.Delivered, FailureKind: null);

        TryLogQuarantined(delivery.Message.Id.Value, attempt, terminalKind, failureType);
        return new PersistedFailureOutcome(DurableSendDeliveryOutcome.Quarantined, terminalKind);
    }

    DurableSendFailureKind Classify(Exception exception)
    {
        foreach (ITransportSendFailureClassifier classifier in _failureClassifiers)
        {
            try
            {
                if (!classifier.TryClassify(exception, out TransportSendFailureKind kind))
                    continue;

                return kind switch
                {
                    TransportSendFailureKind.Transient => DurableSendFailureKind.Transient,
                    TransportSendFailureKind.Permanent => DurableSendFailureKind.NonRetryable,
                    _ => DurableSendFailureKind.Unclassified,
                };
            }
            catch (Exception classifierException)
            {
                // A broken classifier cannot rewrite the delivery outcome. Its own exception text is intentionally not
                // logged because external exception messages are not a safe diagnostics boundary.
                TryLogClassifierFailed(
                    DiagnosticTypeName(classifier.GetType()),
                    DiagnosticTypeName(classifierException.GetType()));
            }
        }

        // Unknown failures are never guessed transient.
        return DurableSendFailureKind.Unclassified;
    }

    static string DiagnosticTypeName(Type type)
    {
        string name = type.FullName ?? type.Name;
        return name.Length <= 512 ? name : name[..512];
    }

    internal TimeSpan CalculateRetryDelay(DurableSendId id, int attempt)
    {
        ReliableMessagingPolicy<TBus> policy = RequirePolicy();
        long ticks = policy.InitialRetryDelay.Ticks;
        long maximumTicks = policy.MaximumRetryDelay.Ticks;

        for (int index = 1; index < attempt && ticks < maximumTicks; index++)
        {
            if (ticks > maximumTicks / 2)
            {
                ticks = maximumTicks;
                break;
            }

            ticks *= 2;
        }

        ticks = Math.Min(ticks, maximumTicks);
        if (policy.RetryJitterFraction == 0)
            return TimeSpan.FromTicks(ticks);

        Span<byte> bytes = stackalloc byte[16];
        id.Value.TryWriteBytes(bytes);
        uint seed = BinaryPrimitives.ReadUInt32LittleEndian(bytes)
            ^ BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..])
            ^ BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..])
            ^ BinaryPrimitives.ReadUInt32LittleEndian(bytes[12..])
            ^ unchecked((uint)attempt * 2654435761u);
        double normalized = seed / (double)uint.MaxValue;

        // Keep jitter even after exponential backoff reaches the configured ceiling. Returning the exact maximum for
        // every sender would re-synchronize a fleet during a long outage and create a retry storm at each interval.
        long lowerTicks = Math.Max(1, (long)Math.Floor(ticks * (1d - policy.RetryJitterFraction)));
        long upperTicks = Math.Min(maximumTicks, (long)Math.Ceiling(ticks * (1d + policy.RetryJitterFraction)));
        long jitteredTicks = lowerTicks == upperTicks
            ? lowerTicks
            : lowerTicks + (long)Math.Floor(normalized * (upperTicks - lowerTicks + 1d));
        return TimeSpan.FromTicks(Math.Clamp(jitteredTicks, lowerTicks, upperTicks));
    }

    readonly record struct PersistedFailureOutcome(
        DurableSendDeliveryOutcome Outcome,
        DurableSendFailureKind? FailureKind);
}
