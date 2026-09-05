using System;
using System.Collections.Generic;

#nullable enable

namespace ViciOne.ServiceBus.Providers.Persistence;

internal sealed class ReliableInboxRetryRequiredException(Exception innerException) :
    Exception("The reliable inbox retained the failed attempt and requires a fresh pipeline invocation.", innerException);

/// <summary>Identifies one consumer's durable processing of one incoming message.</summary>
public readonly record struct ReliableInboxKey(Guid MessageId, Guid ConsumerId)
{
    /// <summary>Validates that both identity components are non-empty.</summary>
    public ReliableInboxKey Validate()
    {
        if (MessageId == Guid.Empty)
            throw new ArgumentException("The inbox message id cannot be empty.", nameof(MessageId));
        if (ConsumerId == Guid.Empty)
            throw new ArgumentException("The inbox consumer id cannot be empty.", nameof(ConsumerId));
        return this;
    }
}

/// <summary>Durable processing state for an inbox record.</summary>
public enum ReliableInboxStatus
{
    /// <summary>The record is owned by a fenced consumer attempt.</summary>
    Processing = 0,
    /// <summary>The consumer transaction committed successfully.</summary>
    Consumed = 1,
    /// <summary>A durable retry is waiting for its due time.</summary>
    RetryScheduled = 2,
    /// <summary>The record requires an operator decision.</summary>
    Quarantined = 3,
    /// <summary>An operator retained the record as deliberately abandoned.</summary>
    Abandoned = 4,
}

/// <summary>Outcome of trying to acquire one inbox identity.</summary>
public enum ReliableInboxAcquireDisposition
{
    /// <summary>The caller owns a new fenced processing attempt.</summary>
    Acquired = 0,
    /// <summary>The same message was already consumed and must not execute again.</summary>
    AlreadyConsumed = 1,
    /// <summary>Another non-expired lease owns the message.</summary>
    Busy = 2,
    /// <summary>The message is quarantined or abandoned.</summary>
    Unavailable = 3,
    /// <summary>The retry exists but is not due yet.</summary>
    NotDue = 4,
}

/// <summary>Fenced ownership issued by an inbox store.</summary>
public readonly record struct ReliableInboxLease(Guid Token, DateTimeOffset ExpiresAt);

/// <summary>Result of acquiring one inbox identity.</summary>
public sealed record ReliableInboxAcquireResult(
    ReliableInboxKey Key,
    ReliableInboxAcquireDisposition Disposition,
    ReliableInboxLease? Lease,
    int Attempt);

/// <summary>Bounded seek-pagination query for inbox quarantine.</summary>
public sealed record ReliableInboxQuarantineQuery
{
    /// <summary>Gets the exclusive timestamp cursor.</summary>
    public DateTimeOffset? AfterQuarantinedAt { get; init; }

    /// <summary>Gets the exclusive message-id cursor paired with the timestamp cursor.</summary>
    public Guid? AfterMessageId { get; init; }

    /// <summary>Gets the exclusive consumer-id cursor paired with the timestamp cursor.</summary>
    public Guid? AfterConsumerId { get; init; }

    /// <summary>Gets the requested page size. The absolute maximum is 1,000.</summary>
    public int PageSize { get; init; } = 100;
}

/// <summary>One durable inbox record requiring an operator decision.</summary>
public sealed record ReliableInboxQuarantineEntry(
    ReliableInboxKey Key,
    ReliableInboxStatus Status,
    int Attempts,
    DateTimeOffset ReceivedAt,
    DateTimeOffset QuarantinedAt,
    string? FailureType);

/// <summary>One bounded inbox-quarantine page.</summary>
public sealed record ReliableInboxQuarantinePage(
    IReadOnlyList<ReliableInboxQuarantineEntry> Entries,
    ReliableInboxQuarantineQuery? Next);

/// <summary>Identifies which side of reliable messaging owns an operator reference.</summary>
public enum ReliableMessageKind
{
    /// <summary>The reference identifies an outbox intent.</summary>
    Outbox = 0,
    /// <summary>The reference identifies an inbox processing record.</summary>
    Inbox = 1,
}

/// <summary>Typed reference accepted by the common reliable-messaging operations API.</summary>
public readonly record struct ReliableMessageReference
{
    ReliableMessageReference(ReliableMessageKind kind, DurableSendId outboxId, ReliableInboxKey inboxKey)
    {
        Kind = kind;
        OutboxId = outboxId;
        InboxKey = inboxKey;
    }

    /// <summary>Gets the referenced side.</summary>
    public ReliableMessageKind Kind { get; }

    /// <summary>Gets the outbox id when <see cref="Kind"/> is <see cref="ReliableMessageKind.Outbox"/>.</summary>
    public DurableSendId OutboxId { get; }

    /// <summary>Gets the inbox key when <see cref="Kind"/> is <see cref="ReliableMessageKind.Inbox"/>.</summary>
    public ReliableInboxKey InboxKey { get; }

    /// <summary>Creates an outbox reference.</summary>
    public static ReliableMessageReference Outbox(DurableSendId id) =>
        new(ReliableMessageKind.Outbox, id, default);

    /// <summary>Creates an inbox reference.</summary>
    public static ReliableMessageReference Inbox(ReliableInboxKey key) =>
        new(ReliableMessageKind.Inbox, default, key);
}

/// <summary>Disposition of an explicit reliable-messaging operator action.</summary>
public enum ReliableMessagingOperationDisposition
{
    /// <summary>The requested state transition was applied.</summary>
    Applied = 0,
    /// <summary>The referenced record does not exist.</summary>
    NotFound = 1,
    /// <summary>The record exists but is not in a compatible state.</summary>
    InvalidState = 2,
}

/// <summary>Typed, idempotent outcome returned by reliable-messaging operator actions.</summary>
public sealed record ReliableMessagingOperationResult(
    ReliableMessageReference Reference,
    ReliableMessagingOperationDisposition Disposition,
    string? PreviousState,
    string? CurrentState);
