using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Diagnostics.Telemetry;
using ViciOne.ServiceBus.Providers.Persistence;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class DurableSendAdmission<TBus> : IDurableSendAdmission<TBus>
    where TBus : class, IBus
{
    readonly IOutboxStore<TBus> _store;
    readonly ReliableMessagingPolicy<TBus> _policy;
    readonly IMessageContractCatalog _contractCatalog;
    readonly TimeProvider _timeProvider;
    readonly ServiceBusInstrumentation<TBus> _instrumentation;

    public DurableSendAdmission(IEnumerable<IOutboxStore<TBus>> stores, ReliableMessagingPolicy<TBus> policy,
        IEnumerable<IMessageContractCatalog> contractCatalogs, TimeProvider timeProvider,
        ServiceBusInstrumentation<TBus> instrumentation)
    {
        _store = ReliableMessagingComposition.RequireExactlyOne<IOutboxStore<TBus>, TBus>(stores, "persistence store");
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _contractCatalog = ReliableMessagingComposition.RequireExactlyOne<IMessageContractCatalog, TBus>(
            contractCatalogs,
            "message-contract catalog");
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
    }

    public Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Validate();

        return AdmitCoreAsync(message, cancellationToken);
    }

    async Task<DurableSendAdmissionResult> AdmitCoreAsync(SerializedDurableSend message, CancellationToken cancellationToken)
    {
        using SafeActivityScope activity = _instrumentation.StartDurableAdmission(message);

        try
        {
            if (!_contractCatalog.TryGetMessageType(message.ContractIdentity, out _))
            {
                throw new MessageContractException(
                    $"Durable send contract identity '{message.ContractIdentity}' is not registered in the immutable message contract catalog.");
            }

            DurableSendAdmissionResult result = await _store
                .AdmitAsync(message, _policy.Limits, _timeProvider.GetUtcNow(), cancellationToken)
                .ConfigureAwait(false);
            activity.SetTag("vicione.servicebus.admission.outcome", result.Disposition.ToString());
            _instrumentation.RecordDurableAdmission(result.Disposition, message.StorageSize);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            activity.SetTag("vicione.servicebus.admission.outcome", "canceled");
            throw;
        }
        catch (MessageContractException)
        {
            activity.SetTag("vicione.servicebus.admission.outcome", "rejected");
            activity.SetFailure("contract-not-registered");
            _instrumentation.RecordDurableAdmissionRejected(
                DurableSendAdmissionFailure.ContractNotRegistered, message.StorageSize);
            throw;
        }
        catch (DurableSendCapacityExceededException)
        {
            activity.SetTag("vicione.servicebus.admission.outcome", "rejected");
            activity.SetFailure("capacity-exceeded");
            _instrumentation.RecordDurableAdmissionRejected(DurableSendAdmissionFailure.CapacityExceeded, message.StorageSize);
            throw;
        }
        catch (DurableSendIdentityConflictException)
        {
            activity.SetTag("vicione.servicebus.admission.outcome", "rejected");
            activity.SetFailure("identity-conflict");
            _instrumentation.RecordDurableAdmissionRejected(DurableSendAdmissionFailure.IdentityConflict, message.StorageSize);
            throw;
        }
        catch
        {
            activity.SetTag("vicione.servicebus.admission.outcome", "failed");
            activity.SetFailure("durable-store-failure");
            _instrumentation.RecordDurableAdmissionRejected(DurableSendAdmissionFailure.StoreFailure, message.StorageSize);
            throw;
        }
    }

}
