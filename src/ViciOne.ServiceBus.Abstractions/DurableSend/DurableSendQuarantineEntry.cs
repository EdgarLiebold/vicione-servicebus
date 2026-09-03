using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Payload-free operational evidence for a quarantined durable send.
/// </summary>
public sealed record DurableSendQuarantineEntry
{
    public required DurableSendId Id { get; init; }
    public required MessageContractIdentity ContractIdentity { get; init; }
    public required Uri DestinationAddress { get; init; }
    public required DateTimeOffset EnqueuedAt { get; init; }
    public required DateTimeOffset QuarantinedAt { get; init; }
    public required int DeliveryAttempts { get; init; }
    public required DurableSendFailureKind FailureKind { get; init; }
    public string? FailureType { get; init; }
}
