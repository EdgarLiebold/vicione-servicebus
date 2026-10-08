using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Monitoring.Telemetry;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class ReliableMessagingDeliveryService<TBus>
    where TBus : class, IBus
{
    async Task DeliverAsync(DurableSendDelivery delivery, CancellationToken cancellationToken)
    {
        long? started = null;
        try
        {
            started = _timeProvider.GetTimestamp();
        }
        catch
        {
            // Optional duration observation cannot prevent dispatch of an already claimed intent.
        }
        using SafeActivityScope activity = _instrumentation.StartDurableDelivery(delivery);

        if (delivery.Status == DurableSendStatus.AwaitingConsumerCompletion
            && delivery.DeliveryAttempts >= RequirePolicy().MaximumDeliveryAttempts)
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
            RecordCanceled(activity, started);
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
                    // false means an in-process completion capability already removed the same durable intent.
                    RecordOutcome(activity, DurableSendDeliveryOutcome.Delivered, failureKind: null, started);
                    return;

                case DurableSendCompletionMode.ConsumerCompletion:
                    {
                        DateTimeOffset dispatchedAt = _timeProvider.GetUtcNow();
                        bool awaiting = await _store.AwaitConsumerCompletionAsync(
                                delivery.Message.Id,
                                delivery.Lease,
                                attempt,
                                dispatchedAt + RequirePolicy().ConsumerCompletionTimeout,
                                cancellationToken)
                            .ConfigureAwait(false);

                        // false means a very fast in-process consumer completed and removed the intent before the delivery
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
            RecordCanceled(activity, started);
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
        long? started,
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
            RecordCanceled(activity, started);
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
            RequirePolicy().MaximumDeliveryAttempts);
        RecordOutcome(
            activity,
            DurableSendDeliveryOutcome.Quarantined,
            DurableSendFailureKind.ConsumerCompletionTimeout,
            started);
    }

    static string DispatchPersistenceOutcome(DurableSendCompletionMode completionMode)
        => completionMode switch
        {
            DurableSendCompletionMode.TransportAcceptance => "transport-accepted",
            DurableSendCompletionMode.ConsumerCompletion => "consumer-completion-await",
            _ => "invalid-completion-mode",
        };
}
