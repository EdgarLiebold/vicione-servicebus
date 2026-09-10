using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class InMemoryReliableStore<TBus>
    where TBus : class, IBus
{
    public Task<DurableSendAdmissionResult> ScheduleAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return AdmitAsync(message with { DueAt = dueAt }, limits, enqueuedAt, cancellationToken);
    }

    public Task<ReliableMessagingOperationResult> CancelAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = ReliableMessageReference.Outbox(id);
        lock (_lock)
        {
            if (!_records.TryGetValue(id, out Record? record))
                return Task.FromResult(NotFound(reference));
            if (record.Lease is not null || record.Status == DurableSendStatus.Quarantined)
                return Task.FromResult(InvalidState(reference, record.Status));

            _records.Remove(id);
            return Task.FromResult(Applied(reference, record.Status, "Canceled"));
        }
    }
}
