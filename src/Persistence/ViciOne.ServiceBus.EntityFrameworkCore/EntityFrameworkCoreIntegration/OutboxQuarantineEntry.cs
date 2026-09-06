using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Describes one quarantined transactional-outbox delivery.</summary>
/// <param name="OutboxId">The identifier of the quarantined outbox.</param>
/// <param name="Created">The UTC time when the outbox row was created.</param>
/// <param name="DeliveryAttempts">The number of consecutive delivery failures.</param>
/// <param name="FailureKind">The classification that caused quarantine.</param>
/// <param name="FailureTime">The UTC time of the failure that caused quarantine.</param>
/// <param name="Failure">Bounded diagnostic text for that failure.</param>
/// <param name="FailedSequenceNumber">The sequence number of the message that failed.</param>
/// <param name="FailedMessageId">The identifier of the message that failed.</param>
public sealed record OutboxQuarantineEntry(
    Guid OutboxId,
    DateTimeOffset Created,
    int DeliveryAttempts,
    OutboxFailureKind FailureKind,
    DateTimeOffset? FailureTime,
    string? Failure,
    long? FailedSequenceNumber,
    Guid? FailedMessageId);
