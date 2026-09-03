namespace ViciOne.ServiceBus.DurableSend;

#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Diagnostics;
using ViciOne.ServiceBus.Transports;

internal sealed partial class DurableSenderDeliveryService<TBus> : BackgroundService
    where TBus : class, IBus
{
    readonly IDurableSendDispatcher<TBus> _dispatcher;
    readonly IReadOnlyList<ITransportSendFailureClassifier> _failureClassifiers;
    readonly V5ServiceBusInstrumentation<TBus> _instrumentation;
    readonly ILogger<DurableSenderDeliveryService<TBus>> _logger;
    readonly DurableSenderPolicy<TBus> _policy;
    readonly IDurableSendStore<TBus> _store;
    readonly TimeProvider _timeProvider;
    long _nextTelemetrySnapshotUtcTicks;

    public DurableSenderDeliveryService(
        IDurableSendStore<TBus> store,
        IDurableSendDispatcher<TBus> dispatcher,
        IEnumerable<ITransportSendFailureClassifier> failureClassifiers,
        DurableSenderPolicy<TBus> policy,
        TimeProvider timeProvider,
        ILogger<DurableSenderDeliveryService<TBus>> logger,
        V5ServiceBusInstrumentation<TBus> instrumentation)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        ArgumentNullException.ThrowIfNull(failureClassifiers);
        _failureClassifiers = failureClassifiers.ToArray();
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool didWork = await DeliverDueBatchAsync(stoppingToken).ConfigureAwait(false);
            if (!didWork)
                await Task.Delay(_policy.PollInterval, _timeProvider, stoppingToken).ConfigureAwait(false);
        }
    }

    internal async Task<bool> DeliverDueBatchAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        // Claim no more work than can begin immediately. A durable lease is ownership, not a local work buffer:
        // pre-claiming a larger batch would leave later records leased-but-idle behind a slow provider send.
        IReadOnlyList<DurableSendDelivery> batch = await _store
            .ClaimDueAsync(now, _policy.MaximumConcurrentDeliveries, _policy.LeaseDuration, cancellationToken)
            .ConfigureAwait(false);

        if (batch.Count == 1)
        {
            await DeliverAsync(batch[0], cancellationToken).ConfigureAwait(false);
        }
        else if (batch.Count > 1)
        {
            var deliveries = new Task[batch.Count];
            for (int index = 0; index < batch.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                deliveries[index] = DeliverAsync(batch[index], cancellationToken);
            }

            await Task.WhenAll(deliveries).ConfigureAwait(false);
        }

        await RefreshTelemetrySnapshotIfDueAsync(_timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        return batch.Count > 0;
    }

    async Task DeliverAsync(DurableSendDelivery delivery, CancellationToken cancellationToken)
    {
        long started = Stopwatch.GetTimestamp();
        using SafeActivityScope activity = _instrumentation.StartDurableDelivery(delivery);

        if (delivery.Status == DurableSendStatus.AwaitingConsumerCompletion
            && delivery.DeliveryAttempts >= _policy.MaximumDeliveryAttempts)
        {
            await QuarantineConsumerCompletionTimeoutAsync(delivery, activity, started, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        int attempt = checked(delivery.DeliveryAttempts + 1);
        var completion = new DurableSendConsumerCompletion<TBus>(
            delivery.Message.Id,
            delivery.GenerationToken,
            _store,
            _timeProvider,
            _instrumentation);
        var dispatchContext = new DurableSendDispatchContext(
            delivery.Message,
            delivery.Message.Id,
            attempt,
            completion);

        DurableSendDispatchResult dispatchResult;
        try
        {
            dispatchResult = await _dispatcher.DispatchAsync(dispatchContext, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RecordCancelled(activity, started);
            throw;
        }
        catch (Exception dispatchException)
        {
            await PersistTransportFailureAsync(delivery, dispatchException, activity, started, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        try
        {
            switch (dispatchResult.CompletionMode)
            {
                case DurableSendCompletionMode.TransportAcceptance:
                    _ = await _store.MarkDeliveredAsync(
                            delivery.Message.Id,
                            delivery.Lease,
                            _timeProvider.GetUtcNow(),
                            cancellationToken)
                        .ConfigureAwait(false);
                    // false means an in-process completion capability already retired the same durable intent.
                    RecordOutcome(activity, DurableSendDeliveryOutcome.Delivered, failureKind: null, started);
                    return;

                case DurableSendCompletionMode.ConsumerCompletion:
                    {
                        DateTimeOffset dispatchedAt = _timeProvider.GetUtcNow();
                        bool awaiting = await _store.AwaitConsumerCompletionAsync(
                                delivery.Message.Id,
                                delivery.Lease,
                                attempt,
                                dispatchedAt + _policy.ConsumerCompletionTimeout,
                                cancellationToken)
                            .ConfigureAwait(false);

                        // false means a very fast in-process consumer completed and retired the intent before the delivery
                        // worker persisted AwaitingConsumerCompletion. This race is a successful terminal outcome.
                        RecordOutcome(
                            activity,
                            awaiting
                                ? DurableSendDeliveryOutcome.AwaitingConsumerCompletion
                                : DurableSendDeliveryOutcome.Delivered,
                            failureKind: null,
                            started);
                        return;
                    }

                default:
                    bool quarantined = await _store.QuarantineAsync(
                            delivery.Message.Id,
                            delivery.Lease,
                            attempt,
                            DurableSendFailureKind.InvariantViolation,
                            "invalid-durable-send-completion-mode",
                            _timeProvider.GetUtcNow(),
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (!quarantined)
                    {
                        RecordOutcome(activity, DurableSendDeliveryOutcome.Delivered, failureKind: null, started);
                        return;
                    }

                    TryLogInvalidCompletionMode(delivery.Message.Id.Value, (int)dispatchResult.CompletionMode);
                    RecordOutcome(
                        activity,
                        DurableSendDeliveryOutcome.Quarantined,
                        DurableSendFailureKind.InvariantViolation,
                        started);
                    return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // A successful volatile/broker dispatch followed by shutdown before the state transition remains safe: the
            // old lease eventually expires and the persisted producer intent can be replayed. At-least-once duplication
            // is preferred to silent loss.
            RecordCancelled(activity, started);
            throw;
        }
        catch (Exception persistenceException)
        {
            RecordStatePersistenceFailure(
                delivery.Message.Id,
                DispatchPersistenceOutcome(dispatchResult.CompletionMode),
                persistenceException,
                activity,
                started);
            throw;
        }
    }

    async Task QuarantineConsumerCompletionTimeoutAsync(
        DurableSendDelivery delivery,
        SafeActivityScope activity,
        long started,
        CancellationToken cancellationToken)
    {
        try
        {
            bool quarantined = await _store.QuarantineAsync(
                    delivery.Message.Id,
                    delivery.Lease,
                    delivery.DeliveryAttempts,
                    DurableSendFailureKind.ConsumerCompletionTimeout,
                    "consumer-completion-timeout",
                    _timeProvider.GetUtcNow(),
                    cancellationToken)
                .ConfigureAwait(false);
            if (!quarantined)
            {
                RecordOutcome(activity, DurableSendDeliveryOutcome.Delivered, failureKind: null, started);
                return;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            RecordCancelled(activity, started);
            throw;
        }
        catch (Exception persistenceException)
        {
            RecordStatePersistenceFailure(
                delivery.Message.Id,
                "consumer-completion-timeout",
                persistenceException,
                activity,
                started);
            throw;
        }

        TryLogConsumerCompletionTimeout(
            delivery.Message.Id.Value,
            delivery.DeliveryAttempts,
            _policy.MaximumDeliveryAttempts);
        RecordOutcome(
            activity,
            DurableSendDeliveryOutcome.Quarantined,
            DurableSendFailureKind.ConsumerCompletionTimeout,
            started);
    }

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
            RecordCancelled(activity, started);
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

    void RecordOutcome(
        SafeActivityScope activity,
        DurableSendDeliveryOutcome outcome,
        DurableSendFailureKind? failureKind,
        long started)
    {
        activity.SetTag("vicione.servicebus.delivery.outcome", OutcomeTag(outcome));
        if (failureKind is not null)
            activity.SetFailure(V5ServiceBusInstrumentation<TBus>.ErrorType(failureKind.Value));
        _instrumentation.RecordDurableDelivery(
            outcome,
            failureKind,
            Stopwatch.GetElapsedTime(started).TotalSeconds);
    }

    void RecordCancelled(SafeActivityScope activity, long started)
        => RecordOutcome(activity, DurableSendDeliveryOutcome.Cancelled, failureKind: null, started);

    static string DispatchPersistenceOutcome(DurableSendCompletionMode completionMode)
        => completionMode switch
        {
            DurableSendCompletionMode.TransportAcceptance => "transport-accepted",
            DurableSendCompletionMode.ConsumerCompletion => "consumer-completion-await",
            _ => "invalid-completion-mode",
        };

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
            Stopwatch.GetElapsedTime(started).TotalSeconds);
        TryLogStatePersistenceFailed(
            id.Value,
            dispatchOutcome,
            DiagnosticTypeName(persistenceException.GetType()));
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

        if (failure == DurableSendFailureKind.Transient && attempt < _policy.MaximumDeliveryAttempts)
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
                _policy.MaximumDeliveryAttempts,
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

    async Task RefreshTelemetrySnapshotIfDueAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        long next = Volatile.Read(ref _nextTelemetrySnapshotUtcTicks);
        if (now.UtcTicks < next)
            return;

        // Advance first. If an observation read fails we try again at the configured cadence rather than hot-looping.
        Volatile.Write(ref _nextTelemetrySnapshotUtcTicks, (now + _policy.TelemetrySnapshotInterval).UtcTicks);

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

        // V4 invariant: unknown failures are never guessed transient.
        return DurableSendFailureKind.Unclassified;
    }

    static string DiagnosticTypeName(Type type)
    {
        string name = type.FullName ?? type.Name;
        return name.Length <= 512 ? name : name[..512];
    }

    internal TimeSpan CalculateRetryDelay(DurableSendId id, int attempt)
    {
        long ticks = _policy.InitialRetryDelay.Ticks;
        long maximumTicks = _policy.MaximumRetryDelay.Ticks;

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
        if (_policy.RetryJitterFraction == 0)
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
        long lowerTicks = Math.Max(1, (long)Math.Floor(ticks * (1d - _policy.RetryJitterFraction)));
        long upperTicks = Math.Min(maximumTicks, (long)Math.Ceiling(ticks * (1d + _policy.RetryJitterFraction)));
        long jitteredTicks = lowerTicks == upperTicks
            ? lowerTicks
            : lowerTicks + (long)Math.Floor(normalized * (upperTicks - lowerTicks + 1d));
        return TimeSpan.FromTicks(Math.Clamp(jitteredTicks, lowerTicks, upperTicks));
    }

    void TryLogRetryScheduled(
        Guid durableSendId,
        int attempt,
        int maximumAttempts,
        double delaySeconds,
        string? failureType)
    {
        try
        {
            LogRetryScheduled(durableSendId, attempt, maximumAttempts, delaySeconds, failureType);
        }
        catch
        {
            // Logging is an observer and cannot change delivery semantics.
        }
    }

    void TryLogQuarantinedNonRetryable(Guid durableSendId, string contractIdentity, string? failureType)
    {
        try
        {
            LogQuarantinedNonRetryable(durableSendId, contractIdentity, failureType);
        }
        catch
        {
        }
    }

    void TryLogQuarantined(Guid durableSendId, int attempt, DurableSendFailureKind failureKind, string? failureType)
    {
        try
        {
            LogQuarantined(durableSendId, attempt, failureKind, failureType);
        }
        catch
        {
        }
    }

    void TryLogClassifierFailed(string classifierType, string classifierFailureType)
    {
        try
        {
            LogClassifierFailed(classifierType, classifierFailureType);
        }
        catch
        {
        }
    }

    void TryLogStatePersistenceFailed(Guid durableSendId, string dispatchOutcome, string persistenceFailureType)
    {
        try
        {
            LogStatePersistenceFailed(durableSendId, dispatchOutcome, persistenceFailureType);
        }
        catch
        {
        }
    }

    void TryLogTelemetrySnapshotFailed(string failureType)
    {
        try
        {
            LogTelemetrySnapshotFailed(failureType);
        }
        catch
        {
        }
    }

    void TryLogConsumerCompletionTimeout(Guid durableSendId, int attempts, int maximumAttempts)
    {
        try
        {
            LogConsumerCompletionTimeout(durableSendId, attempts, maximumAttempts);
        }
        catch
        {
        }
    }

    void TryLogInvalidCompletionMode(Guid durableSendId, int completionMode)
    {
        try
        {
            LogInvalidCompletionMode(durableSendId, completionMode);
        }
        catch
        {
        }
    }

    static string OutcomeTag(DurableSendDeliveryOutcome outcome)
        => outcome switch
        {
            DurableSendDeliveryOutcome.RetryScheduled => "retry-scheduled",
            DurableSendDeliveryOutcome.Quarantined => "quarantined",
            DurableSendDeliveryOutcome.Delivered => "delivered",
            DurableSendDeliveryOutcome.AwaitingConsumerCompletion => "awaiting-consumer-completion",
            DurableSendDeliveryOutcome.Cancelled => "cancelled",
            DurableSendDeliveryOutcome.StatePersistenceFailed => "state-persistence-failed",
            _ => "unknown",
        };

    [LoggerMessage(5001, LogLevel.Debug,
        "Durable send {DurableSendId} scheduled for retry {Attempt}/{MaximumAttempts} after {DelaySeconds}s ({FailureType}).")]
    partial void LogRetryScheduled(Guid durableSendId, int attempt, int maximumAttempts, double delaySeconds, string? failureType);

    [LoggerMessage(5002, LogLevel.Warning,
        "Durable send {DurableSendId} ({ContractIdentity}) was quarantined after a non-retryable transport failure ({FailureType}).")]
    partial void LogQuarantinedNonRetryable(Guid durableSendId, string contractIdentity, string? failureType);

    [LoggerMessage(5003, LogLevel.Warning,
        "Durable send {DurableSendId} was quarantined after attempt {Attempt}: {FailureKind} ({FailureType}).")]
    partial void LogQuarantined(Guid durableSendId, int attempt, DurableSendFailureKind failureKind, string? failureType);

    [LoggerMessage(5004, LogLevel.Warning,
        "Transport failure classifier {ClassifierType} threw {ClassifierFailureType} while classifying a durable send; its result was ignored.")]
    partial void LogClassifierFailed(string classifierType, string classifierFailureType);

    [LoggerMessage(5005, LogLevel.Error,
        "Durable send {DurableSendId} could not persist delivery state after {DispatchOutcome}; durable-state persistence failed with {PersistenceFailureType}.")]
    partial void LogStatePersistenceFailed(Guid durableSendId, string dispatchOutcome, string persistenceFailureType);

    [LoggerMessage(5006, LogLevel.Debug,
        "Durable sender telemetry snapshot refresh failed with {FailureType}; delivery semantics are unaffected.")]
    partial void LogTelemetrySnapshotFailed(string failureType);

    [LoggerMessage(5007, LogLevel.Warning,
        "Durable send {DurableSendId} was quarantined after {Attempts}/{MaximumAttempts} volatile delivery attempts without consumer completion.")]
    partial void LogConsumerCompletionTimeout(Guid durableSendId, int attempts, int maximumAttempts);

    [LoggerMessage(5008, LogLevel.Error,
        "Durable send {DurableSendId} was quarantined because dispatcher returned unsupported completion mode {CompletionMode}.")]
    partial void LogInvalidCompletionMode(Guid durableSendId, int completionMode);

    readonly record struct PersistedFailureOutcome(
        DurableSendDeliveryOutcome Outcome,
        DurableSendFailureKind? FailureKind);
}
