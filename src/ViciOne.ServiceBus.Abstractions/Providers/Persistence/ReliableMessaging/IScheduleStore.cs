using System;
using System.Threading;
using System.Threading.Tasks;
namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Persistence SPI that materializes scheduled work as due outbox intents.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IScheduleStore<TBus>
    where TBus : class, IBus
{
    /// <summary>Atomically admits an outbox intent that becomes claimable at <paramref name="dueAt"/>.</summary>
    /// <param name="message">The validated serialized intent.</param>
    /// <param name="limits">The hard retained-store limits.</param>
    /// <param name="enqueuedAt">The admission timestamp.</param>
    /// <param name="dueAt">The first instant at which the intent may be claimed.</param>
    /// <param name="cancellationToken">The token used to cancel admission.</param>
    /// <returns>A task containing the idempotent admission result.</returns>
    Task<DurableSendAdmissionResult> ScheduleAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a not-yet-delivered scheduled outbox intent.</summary>
    /// <param name="id">The scheduled durable-send identity.</param>
    /// <param name="cancellationToken">The token used to cancel the store operation.</param>
    /// <returns>A task containing the exact cancellation outcome.</returns>
    Task<ReliableMessagingOperationResult> CancelAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);
}
