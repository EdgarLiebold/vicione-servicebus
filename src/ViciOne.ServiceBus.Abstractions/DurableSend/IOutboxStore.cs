using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Persistence SPI for producer-side durable store-and-forward.</summary>
/// <typeparam name="TBus">The bus type.</typeparam>
/// <remarks>
/// Implementations own atomic admission, hard retained-storage enforcement, claim fencing and restart-safe state
/// transitions. Quarantine is retained state and therefore consumes capacity until explicitly discarded. Successful
/// delivery removes the record atomically with capacity release. Lease-based terminal/retry transitions return false only
/// when a concurrent logical consumer completion has already removed the record; an existing record owned by another lease
/// is still a fencing violation. Consumer-completion capabilities are additionally fenced by a persisted generation token,
/// so a stale in-process capability can never remove a later re-admission that reuses the same durable-send id. A durable acceptance may be acknowledged to the caller
/// only after <see cref="AdmitAsync"/> commits successfully.
/// </remarks>
public interface IOutboxStore<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Atomically admits one immutable intent. Implementations that retain the message in memory must snapshot the
    /// payload/metadata bytes before returning successfully; caller-owned buffers are not store-owned lifetime.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="limits">The limits used by the operation.</param>
    /// <param name="enqueuedAt">The enqueued at used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the admit outcome.</returns>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Claims records whose delivery time is due.</summary>
    /// <param name="now">The now.</param>
    /// <param name="maximumCount">The maximum count.</param>
    /// <param name="leaseDuration">The lease duration.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the claim due outcome.</returns>
    Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
        DateTimeOffset now,
        int maximumCount,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>Reports that mark has been delivered.</summary>
    /// <param name="id">The id.</param>
    /// <param name="lease">The lease.</param>
    /// <param name="deliveredAt">The delivered at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the mark delivered outcome.</returns>
    Task<bool> MarkDeliveredAsync(
        DurableSendId id,
        DurableSendLease lease,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken = default);

    /// <summary>Waits for every consumer invoked by the operation to complete.</summary>
    /// <param name="id">The id.</param>
    /// <param name="lease">The lease.</param>
    /// <param name="deliveryAttempts">The delivery attempts.</param>
    /// <param name="nextAttemptAt">The next attempt at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the await consumer completion outcome.</returns>
    Task<bool> AwaitConsumerCompletionAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default);

    /// <summary>Completes consumer delivery.</summary>
    /// <param name="id">The id.</param>
    /// <param name="generationToken">The generation token.</param>
    /// <param name="completedAt">The completed at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the complete consumer delivery outcome.</returns>
    Task<bool> CompleteConsumerDeliveryAsync(
        DurableSendId id,
        Guid generationToken,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Schedules retry.</summary>
    /// <param name="id">The id.</param>
    /// <param name="lease">The lease.</param>
    /// <param name="deliveryAttempts">The delivery attempts.</param>
    /// <param name="nextAttemptAt">The next attempt at.</param>
    /// <param name="failureKind">The failure kind.</param>
    /// <param name="failureType">The runtime failure type used by the operation.</param>
    /// <param name="failedAt">The failed at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule retry outcome.</returns>
    Task<bool> ScheduleRetryAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Moves the selected record to quarantine.</summary>
    /// <param name="id">The id.</param>
    /// <param name="lease">The lease.</param>
    /// <param name="deliveryAttempts">The delivery attempts.</param>
    /// <param name="failureKind">The failure kind.</param>
    /// <param name="failureType">The runtime failure type used by the operation.</param>
    /// <param name="quarantinedAt">The quarantined at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the quarantine outcome.</returns>
    Task<bool> QuarantineAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Gets snapshot.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets quarantine.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<DurableSendQuarantinePage> GetQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current message to its queue.</summary>
    /// <param name="id">The id.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requeue outcome.</returns>
    Task<DurableSendOperationResult> RequeueAsync(
        DurableSendId id,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>Discards quarantined.</summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the discard quarantined outcome.</returns>
    Task<DurableSendOperationResult> DiscardQuarantinedAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);
}
