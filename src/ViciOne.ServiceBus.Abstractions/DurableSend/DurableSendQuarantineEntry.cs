using System;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Payload-free operational evidence for a quarantined durable send.</summary>
public sealed record DurableSendQuarantineEntry
{
    /// <summary>Gets or sets the id.</summary>
    public required DurableSendId Id { get; init; }
    /// <summary>Gets or sets the contract identity.</summary>
    public required MessageContractIdentity ContractIdentity { get; init; }
    /// <summary>Gets or sets the destination address.</summary>
    public required Uri DestinationAddress { get; init; }
    /// <summary>Gets or sets the enqueued at.</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }
    /// <summary>Gets or sets the quarantined at.</summary>
    public required DateTimeOffset QuarantinedAt { get; init; }
    /// <summary>Gets or sets the delivery attempts.</summary>
    public required int DeliveryAttempts { get; init; }
    /// <summary>Gets or sets the failure kind.</summary>
    public required DurableSendFailureKind FailureKind { get; init; }
    /// <summary>Gets or sets the failure type.</summary>
    public string? FailureType { get; init; }
}
