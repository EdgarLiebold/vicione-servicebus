using System;

namespace ViciOne.ServiceBus.Operations;

/// <summary>
/// Payload-free operational evidence for a quarantined durable send.
/// </summary>
public sealed record DurableSendQuarantineEntry
{
    /// <summary>
    /// Gets or sets the id value.
    /// </summary>
    public required DurableSendId Id { get; init; }
    /// <summary>
    /// Gets or sets the contract identity value.
    /// </summary>
    public required MessageContractIdentity ContractIdentity { get; init; }
    /// <summary>
    /// Gets or sets the destination address value.
    /// </summary>
    public required Uri DestinationAddress { get; init; }
    /// <summary>
    /// Gets or sets the enqueued at value.
    /// </summary>
    public required DateTimeOffset EnqueuedAt { get; init; }
    /// <summary>
    /// Gets or sets the quarantined at value.
    /// </summary>
    public required DateTimeOffset QuarantinedAt { get; init; }
    /// <summary>
    /// Gets or sets the delivery attempts value.
    /// </summary>
    public required int DeliveryAttempts { get; init; }
    /// <summary>
    /// Gets or sets the failure kind value.
    /// </summary>
    public required DurableSendFailureKind FailureKind { get; init; }
    /// <summary>
    /// Gets or sets the failure type value.
    /// </summary>
    public string? FailureType { get; init; }
}
