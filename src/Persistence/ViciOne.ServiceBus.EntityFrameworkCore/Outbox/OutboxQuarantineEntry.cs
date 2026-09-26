using System;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Immutable, payload-free evidence for one quarantined transactional outbox.</summary>
public sealed class OutboxQuarantineEntry
{
    internal OutboxQuarantineEntry(
        Guid outboxId,
        DateTimeOffset created,
        int deliveryAttempts,
        OutboxFailureKind failureKind,
        OutboxFailureCode failureCode,
        DateTimeOffset? failureTime,
        string? exceptionType,
        long? failedSequenceNumber,
        Guid? failedMessageId)
    {
        if (outboxId == Guid.Empty)
            throw new ArgumentException("A quarantine entry requires a nonempty outbox identifier.", nameof(outboxId));
        ValidateDeliveryAttempts(deliveryAttempts, failureKind, failureCode);
        if (!Enum.IsDefined(failureKind) || failureKind == OutboxFailureKind.None)
            throw new ArgumentOutOfRangeException(nameof(failureKind), failureKind, "A quarantine entry requires a defined failure classification.");
        if (!Enum.IsDefined(failureCode) || failureCode == OutboxFailureCode.None)
            throw new ArgumentOutOfRangeException(nameof(failureCode), failureCode, "A quarantine entry requires a defined failure reason.");
        if (failureTime is null)
            throw new ArgumentNullException(nameof(failureTime), "A quarantine entry requires a failure timestamp.");
        if (failedSequenceNumber is null or <= 0)
            throw new ArgumentOutOfRangeException(nameof(failedSequenceNumber), failedSequenceNumber, "A quarantine entry requires a positive failed sequence number.");
        ValidateFailedMessageId(failedMessageId, failureKind, failureCode);

        OutboxId = outboxId;
        Created = created;
        DeliveryAttempts = deliveryAttempts;
        FailureKind = failureKind;
        FailureCode = failureCode;
        FailureTime = failureTime.Value;
        ExceptionType = exceptionType;
        FailedSequenceNumber = failedSequenceNumber.Value;
        FailedMessageId = failedMessageId!.Value;
    }

    /// <summary>Gets the identifier of the quarantined outbox.</summary>
    public Guid OutboxId { get; }

    /// <summary>Gets the UTC time when the outbox was created.</summary>
    public DateTimeOffset Created { get; }

    /// <summary>Gets the failure count, retaining an original corrupt counter for classified invariant failures.</summary>
    public int DeliveryAttempts { get; }

    /// <summary>Gets the classification that caused quarantine.</summary>
    public OutboxFailureKind FailureKind { get; }

    /// <summary>Gets the stable reason for the failure.</summary>
    public OutboxFailureCode FailureCode { get; }

    /// <summary>Gets the UTC time of the failure that caused quarantine.</summary>
    public DateTimeOffset FailureTime { get; }

    /// <summary>Gets the associated exception type, when one exists.</summary>
    public string? ExceptionType { get; }

    /// <summary>Gets the sequence number of the message that failed.</summary>
    public long FailedSequenceNumber { get; }

    /// <summary>Gets the failed message identifier, retaining an original empty value for classified invariant failures.</summary>
    public Guid FailedMessageId { get; }

    static void ValidateDeliveryAttempts(int deliveryAttempts, OutboxFailureKind failureKind, OutboxFailureCode failureCode)
    {
        if (deliveryAttempts > 0)
            return;
        if (deliveryAttempts < 0 && IsRetainedInvariantEvidence(failureKind, failureCode))
            return;

        throw new ArgumentOutOfRangeException(nameof(deliveryAttempts), deliveryAttempts,
            "A quarantined outbox requires a delivery attempt or classified evidence of a corrupt attempt counter.");
    }

    static void ValidateFailedMessageId(Guid? failedMessageId, OutboxFailureKind failureKind, OutboxFailureCode failureCode)
    {
        if (failedMessageId is { } id && (id != Guid.Empty || IsRetainedInvariantEvidence(failureKind, failureCode)))
            return;

        throw new ArgumentException("A quarantine entry requires a failed message identifier or classified evidence of a corrupt identifier.",
            nameof(failedMessageId));
    }

    static bool IsRetainedInvariantEvidence(OutboxFailureKind failureKind, OutboxFailureCode failureCode)
        => failureKind == OutboxFailureKind.InvariantViolation
            && failureCode is OutboxFailureCode.InvalidDeliveryAttemptCount
                or OutboxFailureCode.MetadataDeserializationFailed
                or OutboxFailureCode.MissingDestinationAddress;
}
