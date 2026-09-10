using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed partial class InMemoryReliableStore<TBus>
    where TBus : class, IBus
{
    public Task<ReliableInboxAcquireResult> AcquireAsync(
        ReliableInboxKey key,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        key.Validate();
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));

        lock (_lock)
        {
            if (!_inbox.TryGetValue(key, out InboxRecord? record))
            {
                var lease = new ReliableInboxLease(Guid.NewGuid(), now + leaseDuration);
                _inbox.Add(key, new InboxRecord(now, lease));
                return Task.FromResult(new ReliableInboxAcquireResult(
                    key,
                    ReliableInboxAcquireDisposition.Acquired,
                    lease,
                    1));
            }

            if (record.Status == ReliableInboxStatus.Consumed)
            {
                return Task.FromResult(new ReliableInboxAcquireResult(
                    key,
                    ReliableInboxAcquireDisposition.AlreadyConsumed,
                    null,
                    record.Attempts));
            }

            if (record.Status is ReliableInboxStatus.Quarantined or ReliableInboxStatus.Abandoned)
            {
                return Task.FromResult(new ReliableInboxAcquireResult(
                    key,
                    ReliableInboxAcquireDisposition.Unavailable,
                    null,
                    record.Attempts));
            }

            if (record.Status == ReliableInboxStatus.RetryScheduled && record.DueAt > now)
            {
                return Task.FromResult(new ReliableInboxAcquireResult(
                    key,
                    ReliableInboxAcquireDisposition.NotDue,
                    null,
                    record.Attempts));
            }

            if (record.Lease is { } owner && owner.ExpiresAt > now)
            {
                return Task.FromResult(new ReliableInboxAcquireResult(
                    key,
                    ReliableInboxAcquireDisposition.Busy,
                    null,
                    record.Attempts));
            }

            var nextLease = new ReliableInboxLease(Guid.NewGuid(), now + leaseDuration);
            record.Status = ReliableInboxStatus.Processing;
            record.Attempts = checked(record.Attempts + 1);
            record.Lease = nextLease;
            record.DueAt = null;
            return Task.FromResult(new ReliableInboxAcquireResult(
                key,
                ReliableInboxAcquireDisposition.Acquired,
                nextLease,
                record.Attempts));
        }
    }

    public Task<bool> CompleteAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        DateTimeOffset consumedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_inbox.TryGetValue(key, out InboxRecord? record))
                return Task.FromResult(false);
            EnsureInboxOwned(key, record, lease);
            record.Status = ReliableInboxStatus.Consumed;
            record.CompletedAt = consumedAt;
            record.Lease = null;
            record.DueAt = null;
            record.FailureType = null;
            return Task.FromResult(true);
        }
    }

    internal void CompleteConsumer(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        IReadOnlyList<SerializedDurableSend> messages,
        DurableSendStoreLimits limits,
        DateTimeOffset consumedAt)
    {
        ArgumentNullException.ThrowIfNull(messages);

        lock (_lock)
        {
            if (!_inbox.TryGetValue(key, out InboxRecord? inbox))
                throw new InvalidOperationException($"Reliable inbox '{key}' no longer exists.");
            EnsureInboxOwned(key, inbox, lease);

            var additions = new Dictionary<DurableSendId, SerializedDurableSend>();
            foreach (SerializedDurableSend candidate in messages)
            {
                SerializedDurableSend message = Snapshot(candidate.Validate());
                if (_records.TryGetValue(message.Id, out Record? existing))
                {
                    if (!SameIntent(existing.Message, message))
                        throw new DurableSendIdentityConflictException(message.Id);
                    continue;
                }

                if (additions.TryGetValue(message.Id, out SerializedDurableSend? staged))
                {
                    if (!SameIntent(staged, message))
                        throw new DurableSendIdentityConflictException(message.Id);
                    continue;
                }

                additions.Add(message.Id, message);
            }

            var current = CurrentStorage();
            int nextCount = checked(current.Count + additions.Count);
            long additionalBytes = additions.Values.Sum(message => message.StorageSize);
            long nextBytes = checked(current.Bytes + additionalBytes);
            if (nextCount > limits.MaximumStoredCount || nextBytes > limits.MaximumStoredBytes)
            {
                throw new DurableSendCapacityExceededException(
                    $"Reliable messaging storage capacity would be exceeded ({nextCount}/{limits.MaximumStoredCount} records, "
                    + $"{nextBytes}/{limits.MaximumStoredBytes} bytes).",
                    current.Count,
                    current.Bytes);
            }

            foreach ((DurableSendId id, SerializedDurableSend message) in additions)
                _records.Add(id, new Record(message, consumedAt));

            inbox.Status = ReliableInboxStatus.Consumed;
            inbox.CompletedAt = consumedAt;
            inbox.Lease = null;
            inbox.DueAt = null;
            inbox.FailedAt = null;
            inbox.FailureType = null;
        }
    }

    public Task<bool> ScheduleRetryAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        DateTimeOffset dueAt,
        string? failureType,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_inbox.TryGetValue(key, out InboxRecord? record))
                return Task.FromResult(false);
            EnsureInboxOwned(key, record, lease);
            record.Status = ReliableInboxStatus.RetryScheduled;
            record.DueAt = dueAt;
            record.FailureType = BoundFailureType(failureType);
            record.FailedAt = failedAt;
            record.Lease = null;
            return Task.FromResult(true);
        }
    }

    public Task<bool> QuarantineAsync(
        ReliableInboxKey key,
        ReliableInboxLease lease,
        string? failureType,
        DateTimeOffset quarantinedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_inbox.TryGetValue(key, out InboxRecord? record))
                return Task.FromResult(false);
            EnsureInboxOwned(key, record, lease);
            record.Status = ReliableInboxStatus.Quarantined;
            record.QuarantinedAt = quarantinedAt;
            record.FailureType = BoundFailureType(failureType);
            record.FailedAt = quarantinedAt;
            record.DueAt = null;
            record.Lease = null;
            return Task.FromResult(true);
        }
    }

    public Task<ReliableInboxQuarantinePage> GetQuarantineAsync(
        ReliableInboxQuarantineQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = ReliableInboxQuarantinePagination.Validate(query);
        bool completeCursor = query.AfterQuarantinedAt.HasValue;

        lock (_lock)
        {
            IEnumerable<KeyValuePair<ReliableInboxKey, InboxRecord>> candidates = _inbox
                .Where(pair => pair.Value.Status == ReliableInboxStatus.Quarantined);
            if (completeCursor)
            {
                DateTimeOffset afterAt = query.AfterQuarantinedAt!.Value;
                Guid afterMessage = query.AfterMessageId!.Value;
                Guid afterConsumer = query.AfterConsumerId!.Value;
                candidates = candidates.Where(pair =>
                    pair.Value.QuarantinedAt < afterAt
                    || pair.Value.QuarantinedAt == afterAt
                    && (pair.Key.MessageId.CompareTo(afterMessage) > 0
                        || pair.Key.MessageId == afterMessage && pair.Key.ConsumerId.CompareTo(afterConsumer) > 0));
            }

            ReliableInboxQuarantineEntry[] entries = candidates
                .OrderByDescending(pair => pair.Value.QuarantinedAt)
                .ThenBy(pair => pair.Key.MessageId)
                .ThenBy(pair => pair.Key.ConsumerId)
                .Take(checked(query.PageSize + 1))
                .Select(pair => new ReliableInboxQuarantineEntry(
                    pair.Key,
                    pair.Value.Status,
                    pair.Value.Attempts,
                    pair.Value.ReceivedAt,
                    pair.Value.QuarantinedAt!.Value,
                    pair.Value.FailureType))
                .ToArray();
            if (entries.Length <= query.PageSize)
                return Task.FromResult(new ReliableInboxQuarantinePage(entries, null));

            ReliableInboxQuarantineEntry[] page = entries[..query.PageSize];
            ReliableInboxQuarantineEntry last = page[^1];
            var next = query with
            {
                AfterQuarantinedAt = last.QuarantinedAt,
                AfterMessageId = last.Key.MessageId,
                AfterConsumerId = last.Key.ConsumerId,
            };
            return Task.FromResult(new ReliableInboxQuarantinePage(page, next));
        }
    }

    public Task<ReliableMessagingOperationResult> RequeueAsync(
        ReliableInboxKey key,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = ReliableMessageReference.Inbox(key);
        lock (_lock)
        {
            if (!_inbox.TryGetValue(key, out InboxRecord? record))
                return Task.FromResult(NotFound(reference));
            if (record.Status != ReliableInboxStatus.Quarantined)
                return Task.FromResult(InvalidState(reference, record.Status));
            ReliableInboxStatus previous = record.Status;
            record.Status = ReliableInboxStatus.RetryScheduled;
            record.DueAt = dueAt;
            record.QuarantinedAt = null;
            record.Lease = null;
            return Task.FromResult(Applied(reference, previous, record.Status));
        }
    }

    public Task<ReliableMessagingOperationResult> DiscardAsync(
        ReliableInboxKey key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = ReliableMessageReference.Inbox(key);
        lock (_lock)
        {
            if (!_inbox.TryGetValue(key, out InboxRecord? record))
                return Task.FromResult(NotFound(reference));
            if (record.Status != ReliableInboxStatus.Quarantined)
                return Task.FromResult(InvalidState(reference, record.Status));
            _inbox.Remove(key);
            return Task.FromResult(Applied(reference, record.Status, "Discarded"));
        }
    }

    public Task<ReliableMessagingOperationResult> AbandonAsync(
        ReliableInboxKey key,
        DateTimeOffset abandonedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var reference = ReliableMessageReference.Inbox(key);
        lock (_lock)
        {
            if (!_inbox.TryGetValue(key, out InboxRecord? record))
                return Task.FromResult(NotFound(reference));
            if (record.Status != ReliableInboxStatus.Quarantined)
                return Task.FromResult(InvalidState(reference, record.Status));
            ReliableInboxStatus previous = record.Status;
            record.Status = ReliableInboxStatus.Abandoned;
            record.CompletedAt = abandonedAt;
            record.Lease = null;
            return Task.FromResult(Applied(reference, previous, record.Status));
        }
    }
}
