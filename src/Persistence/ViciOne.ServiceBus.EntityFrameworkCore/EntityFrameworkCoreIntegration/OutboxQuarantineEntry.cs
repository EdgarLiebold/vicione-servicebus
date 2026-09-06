using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Describes one quarantined transactional-outbox delivery.
/// </summary>
public sealed record OutboxQuarantineEntry(
    Guid OutboxId,
    DateTimeOffset Created,
    int DeliveryAttempts,
    OutboxFailureKind FailureKind,
    DateTimeOffset? FailureTime,
    string? Failure,
    long? FailedSequenceNumber,
    Guid? FailedMessageId);
