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
    /// <param name="message">The message to process.</param>
    /// <param name="limits">The limits.</param>
    /// <param name="enqueuedAt">The enqueued at.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule outcome.</returns>
    Task<DurableSendAdmissionResult> ScheduleAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a not-yet-delivered scheduled outbox intent.</summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the cancel outcome.</returns>
    Task<ReliableMessagingOperationResult> CancelAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);
}
