using System;
using System.Threading;
using System.Threading.Tasks;


namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Persistence SPI that materializes scheduled work as due outbox intents.</summary>
public interface IScheduleStore<TBus>
    where TBus : class, IBus
{
    /// <summary>Atomically admits an outbox intent that becomes claimable at <paramref name="dueAt"/>.</summary>
    Task<DurableSendAdmissionResult> ScheduleAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a not-yet-delivered scheduled outbox intent.</summary>
    Task<ReliableMessagingOperationResult> CancelAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);
}
