using System;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Payload-free operational evidence for a quarantined durable send.</summary>
public sealed record DurableSendQuarantineEntry
{
    /// <summary>Gets the durable-send identity.</summary>
    public required DurableSendId Id { get; init; }

    /// <summary>Gets the stable application-contract identity.</summary>
    public required MessageContractIdentity ContractIdentity { get; init; }

    /// <summary>Gets the absolute transport destination.</summary>
    public required Uri DestinationAddress { get; init; }

    /// <summary>Gets the original admission timestamp.</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }

    /// <summary>Gets the terminal quarantine timestamp.</summary>
    public required DateTimeOffset QuarantinedAt { get; init; }

    /// <summary>Gets the persisted delivery-attempt count.</summary>
    public required int DeliveryAttempts { get; init; }

    /// <summary>Gets the terminal failure category.</summary>
    public required DurableSendFailureKind FailureKind { get; init; }

    /// <summary>Gets the optional runtime failure type retained as payload-free evidence.</summary>
    public string? FailureType { get; init; }

    internal DurableSendQuarantineEntry Validate()
    {
        if (Id.Value == Guid.Empty)
            throw new ArgumentException("A quarantine entry requires a durable-send identity.", nameof(Id));
        if (string.IsNullOrWhiteSpace(ContractIdentity.Name) || ContractIdentity.MajorVersion < 1)
            throw new ArgumentException("A quarantine entry requires a valid contract identity.", nameof(ContractIdentity));
        if (DestinationAddress is null || !DestinationAddress.IsAbsoluteUri)
            throw new ArgumentException("A quarantine entry requires an absolute destination address.", nameof(DestinationAddress));
        if (QuarantinedAt < EnqueuedAt)
            throw new ArgumentException("A durable intent cannot be quarantined before it was admitted.", nameof(QuarantinedAt));
        if (!Enum.IsDefined(FailureKind) || FailureKind == DurableSendFailureKind.None)
            throw new ArgumentException("A quarantine entry requires a terminal failure category.", nameof(FailureKind));
        if (FailureType is not null && string.IsNullOrWhiteSpace(FailureType))
            throw new ArgumentException("A durable-send failure type cannot be empty or contain only white-space characters.", nameof(FailureType));

        ArgumentOutOfRangeException.ThrowIfLessThan(DeliveryAttempts, 1);
        return this;
    }
}
