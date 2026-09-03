using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Persistence SPI for producer-side durable store-and-forward.
/// </summary>
/// <remarks>
/// Implementations own atomic admission, hard retained-storage enforcement, claim fencing and restart-safe state
/// transitions. Quarantine is retained state and therefore consumes capacity until explicitly discarded. Successful
/// delivery removes the record atomically with capacity release. Lease-based terminal/retry transitions return false only
/// when a concurrent logical consumer completion has already retired the record; an existing record owned by another lease
/// is still a fencing violation. Consumer-completion capabilities are additionally fenced by a persisted generation token,
/// so a stale in-process capability can never retire a later re-admission that reuses the same durable-send id. A durable acceptance may be acknowledged to the caller
/// only after <see cref="AdmitAsync"/> commits successfully.
/// </remarks>
public interface IDurableSendStore<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Atomically admits one immutable intent. Implementations that retain the message in memory must snapshot the
    /// payload/metadata bytes before returning successfully; caller-owned buffers are not store-owned lifetime.
    /// </summary>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
        DateTimeOffset now,
        int maximumCount,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<bool> MarkDeliveredAsync(
        DurableSendId id,
        DurableSendLease lease,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken = default);

    Task<bool> AwaitConsumerCompletionAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteConsumerDeliveryAsync(
        DurableSendId id,
        Guid generationToken,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    Task<bool> ScheduleRetryAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default);

    Task<bool> QuarantineAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default);

    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DurableSendQuarantineEntry>> GetQuarantineAsync(
        int maximumCount,
        CancellationToken cancellationToken = default);

    Task<bool> RequeueAsync(DurableSendId id, DateTimeOffset dueAt, CancellationToken cancellationToken = default);

    Task<bool> DiscardQuarantinedAsync(DurableSendId id, CancellationToken cancellationToken = default);
}
