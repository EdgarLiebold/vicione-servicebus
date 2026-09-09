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
/// delivery removes the record atomically with capacity release. Lease-based terminal and retry transitions return
/// <see langword="false" /> only when concurrent logical consumer completion already removed the record; another lease
/// owning an existing record remains a fencing violation. Consumer-completion capabilities are additionally fenced by
/// a persisted generation token, so a stale process-local capability cannot remove a later re-admission using the same
/// durable-send identity. Admission may be acknowledged only after <see cref="AdmitAsync" /> commits successfully.
/// </remarks>
public interface IOutboxStore<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Atomically admits one immutable intent. Implementations that retain the message in memory must snapshot the
    /// payload/metadata bytes before returning successfully; caller-owned buffers are not store-owned lifetime.
    /// </summary>
    /// <param name="message">The validated serialized intent.</param>
    /// <param name="limits">The hard retained-store limits.</param>
    /// <param name="enqueuedAt">The admission timestamp owned by the store.</param>
    /// <param name="cancellationToken">The token used to cancel admission.</param>
    /// <returns>A task containing the idempotent admission result.</returns>
    Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Claims at most <paramref name="maximumCount" /> records whose delivery time is due.</summary>
    /// <param name="now">The authoritative claim time.</param>
    /// <param name="maximumCount">The bounded maximum number of records to claim.</param>
    /// <param name="leaseDuration">The exclusive delivery duration.</param>
    /// <param name="cancellationToken">The token used to cancel claiming.</param>
    /// <returns>A task containing the claimed, fenced deliveries.</returns>
    Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
        DateTimeOffset now,
        int maximumCount,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>Removes an intent after its transport reached a durable acceptance boundary.</summary>
    /// <param name="id">The delivered durable-send identity.</param>
    /// <param name="lease">The fencing lease held by the dispatcher.</param>
    /// <param name="deliveredAt">The durable transport-acceptance timestamp.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing whether this lease completed the transition.</returns>
    Task<bool> MarkDeliveredAsync(
        DurableSendId id,
        DurableSendLease lease,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken = default);

    /// <summary>Persists that a volatile transport accepted the intent and logical consumer completion is outstanding.</summary>
    /// <param name="id">The dispatched durable-send identity.</param>
    /// <param name="lease">The fencing lease held by the dispatcher.</param>
    /// <param name="deliveryAttempts">The updated delivery-attempt count.</param>
    /// <param name="nextAttemptAt">The timeout after which another delivery attempt becomes eligible.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing whether this lease completed the transition.</returns>
    Task<bool> AwaitConsumerCompletionAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default);

    /// <summary>Removes an intent after its process-local consumer pipeline completed successfully.</summary>
    /// <param name="id">The consumed durable-send identity.</param>
    /// <param name="generationToken">The persisted incarnation token carried by the completion capability.</param>
    /// <param name="completedAt">The logical consumer-completion timestamp.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing whether the matching generation was removed.</returns>
    Task<bool> CompleteConsumerDeliveryAsync(
        DurableSendId id,
        Guid generationToken,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Persists a bounded retry after a transient delivery failure.</summary>
    /// <param name="id">The failed durable-send identity.</param>
    /// <param name="lease">The fencing lease held by the failed dispatcher.</param>
    /// <param name="deliveryAttempts">The updated delivery-attempt count.</param>
    /// <param name="nextAttemptAt">The earliest time at which another claim may occur.</param>
    /// <param name="failureKind">The classified failure category.</param>
    /// <param name="failureType">The optional runtime failure type retained as payload-free evidence.</param>
    /// <param name="failedAt">The failure timestamp.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing whether this lease scheduled the retry.</returns>
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
    /// <param name="id">The failed durable-send identity.</param>
    /// <param name="lease">The fencing lease held by the failed dispatcher.</param>
    /// <param name="deliveryAttempts">The updated delivery-attempt count.</param>
    /// <param name="failureKind">The terminal failure category.</param>
    /// <param name="failureType">The optional runtime failure type retained as payload-free evidence.</param>
    /// <param name="quarantinedAt">The quarantine timestamp.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing whether this lease quarantined the intent.</returns>
    Task<bool> QuarantineAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default);

    /// <summary>Gets a bounded aggregate snapshot of retained durable-send state.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task containing the aggregate store snapshot.</returns>
    Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets one bounded payload-free quarantine page.</summary>
    /// <param name="query">The bounded seek-pagination query.</param>
    /// <param name="cancellationToken">The token used to cancel retrieval.</param>
    /// <returns>A task containing the requested quarantine page.</returns>
    Task<DurableSendQuarantinePage> GetQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a quarantined intent to the delivery queue.</summary>
    /// <param name="id">The quarantined durable-send identity.</param>
    /// <param name="dueAt">The earliest time at which delivery may resume.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing the exact state-transition result.</returns>
    Task<DurableSendOperationResult> RequeueAsync(
        DurableSendId id,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);

    /// <summary>Permanently removes a quarantined durable intent.</summary>
    /// <param name="id">The quarantined durable-send identity.</param>
    /// <param name="cancellationToken">The token used to cancel persistence.</param>
    /// <returns>A task containing the exact state-transition result.</returns>
    Task<DurableSendOperationResult> DiscardQuarantinedAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default);
}
