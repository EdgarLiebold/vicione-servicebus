using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class InMemoryReliableStore<TBus>
    where TBus : class, IBus
{
    public Task<DurableSendAdmissionResult> AdmitAsync(
        SerializedDurableSend message,
        DurableSendStoreLimits limits,
        DateTimeOffset enqueuedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(message);
        message.Validate();

        lock (_lock)
        {
            if (_records.TryGetValue(message.Id, out var existing))
            {
                if (!SameIntent(existing.Message, message))
                    throw new DurableSendIdentityConflictException(message.Id);

                var current = CurrentStorage();
                var disposition = existing.Status == DurableSendStatus.Quarantined
                    ? DurableSendAdmissionDisposition.AlreadyQuarantined
                    : DurableSendAdmissionDisposition.AlreadyAccepted;
                return Task.FromResult(new DurableSendAdmissionResult(message.Id, disposition, current.Count, current.Bytes));
            }

            var storage = CurrentStorage();
            var nextCount = checked(storage.Count + 1);
            var nextBytes = checked(storage.Bytes + message.StorageSize);
            if (nextCount > limits.MaximumStoredCount || nextBytes > limits.MaximumStoredBytes)
            {
                throw new DurableSendCapacityExceededException(
                    $"Durable sender storage capacity would be exceeded ({nextCount}/{limits.MaximumStoredCount} records, "
                    + $"{nextBytes}/{limits.MaximumStoredBytes} bytes).",
                    storage.Count,
                    storage.Bytes);
            }

            _records.Add(message.Id, new Record(Snapshot(message), enqueuedAt));
            return Task.FromResult(new DurableSendAdmissionResult(
                message.Id,
                DurableSendAdmissionDisposition.Accepted,
                nextCount,
                nextBytes));
        }
    }

    public Task<IReadOnlyList<DurableSendDelivery>> ClaimDueAsync(
        DateTimeOffset now,
        int maximumCount,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DurableSendOperationLimits.ValidateClaimCount(maximumCount, nameof(maximumCount));
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));

        lock (_lock)
        {
            var result = new List<DurableSendDelivery>(Math.Min(maximumCount, _records.Count));
            foreach (var record in _records.Values
                         .Where(record => record.IsDue(now))
                         .OrderBy(record => record.EnqueuedAt)
                         .ThenBy(record => record.Message.Id.Value)
                         .Take(maximumCount))
            {
                var lease = new DurableSendLease(Guid.NewGuid(), now + leaseDuration);
                record.Lease = lease;
                result.Add(new DurableSendDelivery
                {
                    Message = record.Message,
                    GenerationToken = record.GenerationToken,
                    EnqueuedAt = record.EnqueuedAt,
                    DeliveryAttempts = record.DeliveryAttempts,
                    Status = record.Status,
                    Lease = lease,
                });
            }

            return Task.FromResult<IReadOnlyList<DurableSendDelivery>>(result);
        }
    }

    public Task<bool> MarkDeliveredAsync(
        DurableSendId id,
        DurableSendLease lease,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            _ = deliveredAt;
            if (!_records.TryGetValue(id, out Record? record))
                return Task.FromResult(false);

            EnsureOwned(id, record, lease);
            _records.Remove(id);
            return Task.FromResult(true);
        }
    }

    public Task<bool> AwaitConsumerCompletionAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_records.TryGetValue(id, out Record? record))
                return Task.FromResult(false); // Consumer completion won the dispatch/transition race.
            if (record.Lease is null || record.Lease.Value.Token != lease.Token)
                throw new InvalidOperationException($"Durable send '{id}' is not owned by lease '{lease.Token}'.");

            record.Status = DurableSendStatus.AwaitingConsumerCompletion;
            record.DeliveryAttempts = deliveryAttempts;
            record.NextAttemptAt = nextAttemptAt;
            record.LastFailureKind = DurableSendFailureKind.None;
            record.LastFailureType = null;
            record.LastFailureAt = null;
            record.Lease = null;
            return Task.FromResult(true);
        }
    }

    public Task<bool> CompleteConsumerDeliveryAsync(
        DurableSendId id,
        Guid generationToken,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (generationToken == Guid.Empty)
            throw new ArgumentException("Generation token must not be empty.", nameof(generationToken));

        lock (_lock)
        {
            _ = completedAt;
            if (!_records.TryGetValue(id, out Record? record) || record.GenerationToken != generationToken)
                return Task.FromResult(false);

            // Consumer completion is stronger evidence than an overlapping send-side failure/quarantine transition,
            // but only for the same persisted incarnation. A stale capability from a discarded incarnation must not
            // remove a later re-admission that intentionally reuses the durable-send id.
            return Task.FromResult(_records.Remove(id));
        }
    }

    public Task<bool> ScheduleRetryAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DateTimeOffset nextAttemptAt,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_records.TryGetValue(id, out Record? record))
                return Task.FromResult(false);

            EnsureOwned(id, record, lease);
            record.Status = DurableSendStatus.RetryScheduled;
            record.DeliveryAttempts = deliveryAttempts;
            record.NextAttemptAt = nextAttemptAt;
            record.LastFailureKind = failureKind;
            record.LastFailureType = BoundFailureType(failureType);
            record.LastFailureAt = failedAt;
            record.Lease = null;
            return Task.FromResult(true);
        }
    }

    public Task<bool> QuarantineAsync(
        DurableSendId id,
        DurableSendLease lease,
        int deliveryAttempts,
        DurableSendFailureKind failureKind,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_records.TryGetValue(id, out Record? record))
                return Task.FromResult(false);

            EnsureOwned(id, record, lease);
            record.Status = DurableSendStatus.Quarantined;
            record.DeliveryAttempts = deliveryAttempts;
            record.LastFailureKind = failureKind;
            record.LastFailureType = BoundFailureType(failureType);
            record.LastFailureAt = quarantinedAt;
            record.QuarantinedAt = quarantinedAt;
            record.NextAttemptAt = null;
            record.Lease = null;
            return Task.FromResult(true);
        }
    }

    public Task<DurableSendStoreSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var storage = CurrentStorage();
            var pending = _records.Values.Where(IsDeliverable).ToArray();
            return Task.FromResult(new DurableSendStoreSnapshot(
                storage.Count,
                storage.Bytes,
                pending.Length,
                pending.Count(record => record.Status == DurableSendStatus.RetryScheduled),
                pending.Count(record => record.Status == DurableSendStatus.AwaitingConsumerCompletion),
                _records.Values.Count(record => record.Status == DurableSendStatus.Quarantined),
                pending.Length == 0 ? null : pending.Min(record => record.EnqueuedAt)));
        }
    }

    public Task<DurableSendQuarantinePage> GetQuarantineAsync(
        DurableSendQuarantineQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DurableSendQuarantineSeek seek = DurableSendQuarantinePagination.Validate(query);

        lock (_lock)
        {
            IEnumerable<Record> candidates = _records.Values
                .Where(record => record.Status == DurableSendStatus.Quarantined);
            if (seek.HasValue)
            {
                candidates = candidates.Where(record =>
                    record.QuarantinedAt < seek.QuarantinedAt
                    || record.QuarantinedAt == seek.QuarantinedAt
                    && record.Message.Id.Value.CompareTo(seek.Id.Value) > 0);
            }

            DurableSendQuarantineEntry[] fetched = candidates
                .OrderByDescending(record => record.QuarantinedAt)
                .ThenBy(record => record.Message.Id.Value)
                .Take(checked(query.PageSize + 1))
                .Select(record => new DurableSendQuarantineEntry
                {
                    Id = record.Message.Id,
                    ContractIdentity = record.Message.ContractIdentity,
                    DestinationAddress = record.Message.DestinationAddress,
                    EnqueuedAt = record.EnqueuedAt,
                    QuarantinedAt = record.QuarantinedAt!.Value,
                    DeliveryAttempts = record.DeliveryAttempts,
                    FailureKind = record.LastFailureKind,
                    FailureType = record.LastFailureType,
                })
                .ToArray();
            return Task.FromResult(DurableSendQuarantinePagination.CreatePage(fetched, query.PageSize));
        }
    }

    public Task<DurableSendOperationResult> RequeueAsync(
        DurableSendId id,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_records.TryGetValue(id, out var record))
                return Task.FromResult(new DurableSendOperationResult(id, DurableSendOperationOutcome.NotFound));
            if (record.Status != DurableSendStatus.Quarantined)
                return Task.FromResult(new DurableSendOperationResult(id, DurableSendOperationOutcome.NotQuarantined));

            // Quarantine already consumes retained-storage capacity. Requeue is therefore a pure state transition and
            // can never bypass or overshoot the hard store bound.
            record.Status = DurableSendStatus.RetryScheduled;
            record.DeliveryAttempts = 0;
            record.NextAttemptAt = dueAt;
            record.QuarantinedAt = null;
            record.LastFailureKind = DurableSendFailureKind.None;
            record.LastFailureType = null;
            record.LastFailureAt = null;
            record.Lease = null;
            return Task.FromResult(new DurableSendOperationResult(id, DurableSendOperationOutcome.Requeued));
        }
    }

    public Task<DurableSendOperationResult> DiscardQuarantinedAsync(
        DurableSendId id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_records.TryGetValue(id, out var record))
                return Task.FromResult(new DurableSendOperationResult(id, DurableSendOperationOutcome.NotFound));
            if (record.Status != DurableSendStatus.Quarantined)
                return Task.FromResult(new DurableSendOperationResult(id, DurableSendOperationOutcome.NotQuarantined));

            if (!_records.Remove(id))
                throw new InvalidOperationException($"Durable send '{id}' disappeared while its discard operation owned the store lock.");

            return Task.FromResult(new DurableSendOperationResult(id, DurableSendOperationOutcome.Discarded));
        }
    }
}
