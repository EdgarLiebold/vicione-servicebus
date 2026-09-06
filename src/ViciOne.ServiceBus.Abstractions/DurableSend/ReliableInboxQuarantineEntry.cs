namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>One durable inbox record requiring an operator decision.</summary>
public sealed record ReliableInboxQuarantineEntry(
    ReliableInboxKey Key,
    ReliableInboxStatus Status,
    int Attempts,
    DateTimeOffset ReceivedAt,
    DateTimeOffset QuarantinedAt,
    string? FailureType);
