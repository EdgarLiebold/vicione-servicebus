namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>One durable inbox record requiring an operator decision.</summary>
/// <param name="Key">The key.</param>
/// <param name="Status">The status.</param>
/// <param name="Attempts">The attempts.</param>
/// <param name="ReceivedAt">The received at.</param>
/// <param name="QuarantinedAt">The quarantined at.</param>
/// <param name="FailureType">The runtime failure type used by the operation.</param>
public sealed record ReliableInboxQuarantineEntry(
    ReliableInboxKey Key,
    ReliableInboxStatus Status,
    int Attempts,
    DateTimeOffset ReceivedAt,
    DateTimeOffset QuarantinedAt,
    string? FailureType);
