namespace ViciOne.ServiceBus.Operations;

/// <summary>One durable inbox record requiring an operator decision.</summary>
/// <param name="Key">The incoming-message and consumer identity.</param>
/// <param name="Status">The retained terminal state.</param>
/// <param name="Attempts">The persisted processing-attempt count.</param>
/// <param name="ReceivedAt">The original receive timestamp.</param>
/// <param name="QuarantinedAt">The quarantine timestamp.</param>
/// <param name="FailureType">The optional runtime failure type retained as payload-free evidence.</param>
public sealed record ReliableInboxQuarantineEntry(
    ReliableInboxKey Key,
    ReliableInboxStatus Status,
    int Attempts,
    DateTimeOffset ReceivedAt,
    DateTimeOffset QuarantinedAt,
    string? FailureType)
{
    internal ReliableInboxQuarantineEntry Validate()
    {
        _ = Key.Validate();
        if (Status is not (ReliableInboxStatus.Quarantined or ReliableInboxStatus.Abandoned))
            throw new ArgumentException("An inbox quarantine entry requires a terminal retained state.", nameof(Status));
        if (QuarantinedAt < ReceivedAt)
            throw new ArgumentException("An inbox record cannot be quarantined before it was received.", nameof(QuarantinedAt));
        if (FailureType is not null && string.IsNullOrWhiteSpace(FailureType))
            throw new ArgumentException("An inbox failure type cannot be empty or contain only white-space characters.", nameof(FailureType));

        ArgumentOutOfRangeException.ThrowIfLessThan(Attempts, 1);
        return this;
    }
}
