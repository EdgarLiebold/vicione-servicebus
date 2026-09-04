using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Persistence SPI for producer-side durable store-and-forward.
/// </summary>
/// <remarks>
/// Implementations own atomic admission, hard retained-storage enforcement, claim fencing and restart-safe state
/// transitions. Quarantine is retained state and therefore consumes capacity until explicitly discarded. Successful
/// delivery removes the record atomically with capacity release. Lease-based terminal/retry transitions return false only
/// when a concurrent logical consumer completion has already removed the record; an existing record owned by another lease
/// is still a fencing violation. Consumer-completion capabilities are additionally fenced by a persisted generation token,
/// so a stale in-process capability can never remove a later re-admission that reuses the same durable-send id. A durable acceptance may be acknowledged to the caller
/// only after <see cref="AdmitAsync"/> commits successfully.
/// </remarks>
public interface IDurableSendStore<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Atomically admits one immutable intent. Implementations that retain the message in memory must snapshot the
    /// payload/metadata bytes before returning successfully; caller-owned buffers are not store-owned lifetime.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="limits">The limits used by the operation.</param>
    /// <param name="enqueuedAt">The enqueued at used by the operation.</param>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the claim due operation.
    /// </summary>
    /// <param name="now">The now value.</param>
    /// <param name="maximumCount">The maximum count value.</param>
    /// <param name="leaseDuration">The lease duration value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
        DateTimeOffset now,
        int maximumCount,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the mark delivered operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="lease">The lease value.</param>
    /// <param name="deliveredAt">The delivered at value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> MarkDeliveredAsync(
        DurableSendId id,
        DurableSendLease lease,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the await consumer completion operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="lease">The lease value.</param>
    /// <param name="deliveryAttempts">The delivery attempts value.</param>
    /// <param name="nextAttemptAt">The next attempt at value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> AwaitConsumerCompletionAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the complete consumer delivery operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="generationToken">The generation token value.</param>
    /// <param name="completedAt">The completed at value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> CompleteConsumerDeliveryAsync(
        DurableSendId id,
        Guid generationToken,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules retry.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="lease">The lease value.</param>
    /// <param name="deliveryAttempts">The delivery attempts value.</param>
    /// <param name="nextAttemptAt">The next attempt at value.</param>
    /// <param name="failureKind">The failure kind value.</param>
    /// <param name="failureType">The failure type value.</param>
    /// <param name="failedAt">The failed at value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> ScheduleRetryAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the quarantine operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="lease">The lease value.</param>
    /// <param name="deliveryAttempts">The delivery attempts value.</param>
    /// <param name="failureKind">The failure kind value.</param>
    /// <param name="failureType">The failure type value.</param>
    /// <param name="quarantinedAt">The quarantined at value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<bool> QuarantineAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets snapshot.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets quarantine.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendQuarantinePage> GetQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the requeue operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="dueAt">The due at value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendOperationResult> RequeueAsync(
        DurableSendId id,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the discard quarantined operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<DurableSendOperationResult> DiscardQuarantinedAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);
}
