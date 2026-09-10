using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>
/// Provides bounded, process-local reliable-messaging state with the same atomicity and fencing semantics as a durable
/// provider. All state is lost when the process ends, so this implementation is not a production durability substitute.
/// </summary>
/// <typeparam name="TBus">The bus whose isolated in-memory state is retained.</typeparam>
internal sealed partial class InMemoryReliableStore<TBus> :
    IOutboxStore<TBus>,
    IInboxStore<TBus>,
    IScheduleStore<TBus>
    where TBus : class, IBus
{
    readonly Lock _lock = new();
    readonly Dictionary<ReliableInboxKey, InboxRecord> _inbox = new();
    readonly Dictionary<DurableSendId, Record> _records = new();

    static SerializedDurableSend Snapshot(SerializedDurableSend message)
        => message with
        {
            Body = message.Body.IsEmpty ? ReadOnlyMemory<byte>.Empty : message.Body.ToArray(),
            Metadata = message.Metadata.IsEmpty ? ReadOnlyMemory<byte>.Empty : message.Metadata.ToArray(),
        };

    static void EnsureOwned(DurableSendId id, Record record, DurableSendLease lease)
    {
        if (record.Lease is null || record.Lease.Value.Token != lease.Token)
            throw new InvalidOperationException($"Durable send '{id}' is not owned by lease '{lease.Token}'.");
    }

    (int Count, long Bytes) CurrentStorage()
        => (_records.Count, _records.Values.Sum(record => record.Message.StorageSize));

    static bool IsDeliverable(Record record)
        => record.Status is DurableSendStatus.Pending
            or DurableSendStatus.RetryScheduled
            or DurableSendStatus.AwaitingConsumerCompletion;

    static bool SameIntent(SerializedDurableSend left, SerializedDurableSend right)
        => left.ContractIdentity == right.ContractIdentity
            && left.DestinationAddress == right.DestinationAddress
            && string.Equals(left.ContentType, right.ContentType, StringComparison.Ordinal)
            && left.MessageId == right.MessageId
            && left.CorrelationId == right.CorrelationId
            && left.DueAt == right.DueAt
            && left.Body.Span.SequenceEqual(right.Body.Span)
            && left.Metadata.Span.SequenceEqual(right.Metadata.Span);

    static string? BoundFailureType(string? failureType)
        => failureType is { Length: > 512 } ? failureType[..512] : failureType;

    static void EnsureInboxOwned(ReliableInboxKey key, InboxRecord record, ReliableInboxLease lease)
    {
        if (record.Lease is null || record.Lease.Value.Token != lease.Token)
            throw new InvalidOperationException($"Reliable inbox '{key}' is not owned by lease '{lease.Token}'.");
    }

    static ReliableMessagingOperationResult Applied(
        ReliableMessageReference reference,
        object previous,
        object current) => new(
        reference,
        ReliableMessagingOperationDisposition.Applied,
        previous.ToString(),
        current.ToString());

    static ReliableMessagingOperationResult NotFound(ReliableMessageReference reference) => new(
        reference,
        ReliableMessagingOperationDisposition.NotFound,
        null,
        null);

    static ReliableMessagingOperationResult InvalidState(ReliableMessageReference reference, object state) => new(
        reference,
        ReliableMessagingOperationDisposition.InvalidState,
        state.ToString(),
        state.ToString());

    sealed class Record(SerializedDurableSend message, DateTimeOffset enqueuedAt)
    {
        public SerializedDurableSend Message { get; } = message;
        public Guid GenerationToken { get; } = Guid.NewGuid();
        public DateTimeOffset EnqueuedAt { get; } = enqueuedAt;
        public DurableSendStatus Status { get; set; } = DurableSendStatus.Pending;
        public int DeliveryAttempts { get; set; }
        public DateTimeOffset? NextAttemptAt { get; set; } = message.DueAt;
        public DurableSendLease? Lease { get; set; }
        public DateTimeOffset? QuarantinedAt { get; set; }
        public DurableSendFailureKind LastFailureKind { get; set; }
        public string? LastFailureType { get; set; }
        public DateTimeOffset? LastFailureAt { get; set; }

        public bool IsDue(DateTimeOffset now)
        {
            if (Status == DurableSendStatus.Pending)
            {
                return (NextAttemptAt is null || NextAttemptAt <= now)
                    && (Lease is null || Lease.Value.ExpiresAt <= now);
            }

            if (Status is not (DurableSendStatus.RetryScheduled or DurableSendStatus.AwaitingConsumerCompletion)
                || NextAttemptAt > now)
            {
                return false;
            }

            return Lease is null || Lease.Value.ExpiresAt <= now;
        }
    }

    sealed class InboxRecord(DateTimeOffset receivedAt, ReliableInboxLease lease)
    {
        public DateTimeOffset ReceivedAt { get; } = receivedAt;
        public ReliableInboxStatus Status { get; set; } = ReliableInboxStatus.Processing;
        public int Attempts { get; set; } = 1;
        public ReliableInboxLease? Lease { get; set; } = lease;
        public DateTimeOffset? DueAt { get; set; }
        public DateTimeOffset? FailedAt { get; set; }
        public DateTimeOffset? QuarantinedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string? FailureType { get; set; }
    }
}
