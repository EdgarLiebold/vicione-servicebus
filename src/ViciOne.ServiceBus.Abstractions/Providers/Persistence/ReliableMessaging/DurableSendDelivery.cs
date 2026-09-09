using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Persisted durable-send record claimed for one delivery attempt.</summary>
public sealed record DurableSendDelivery
{
    /// <summary>Gets the immutable durable-send intent claimed for delivery.</summary>
    public required SerializedDurableSend Message { get; init; }

    /// <summary>Gets the persisted incarnation token that changes when a discarded identity is admitted again.</summary>
    public required Guid GenerationToken { get; init; }

    /// <summary>Gets the time at which the current durable intent was admitted.</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }

    /// <summary>Gets the number of delivery attempts persisted before this claim.</summary>
    public required int DeliveryAttempts { get; init; }

    /// <summary>Gets the persisted state held by this claim.</summary>
    public required DurableSendStatus Status { get; init; }

    /// <summary>Gets the exclusive lease that fences state transitions made by this delivery attempt.</summary>
    public required DurableSendLease Lease { get; init; }

    /// <summary>Validates a claimed delivery before it is dispatched outside the persistence provider.</summary>
    /// <returns>This delivery when every provider invariant is satisfied.</returns>
    /// <exception cref="ArgumentException">An identity, generation, lease, or status invariant is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="DeliveryAttempts" /> is negative.</exception>
    public DurableSendDelivery Validate()
    {
        ArgumentNullException.ThrowIfNull(Message);
        _ = Message.Validate();
        if (GenerationToken == Guid.Empty)
            throw new ArgumentException("A claimed durable delivery requires a generation token.", nameof(GenerationToken));
        if (Lease.Token == Guid.Empty)
            throw new ArgumentException("A claimed durable delivery requires a lease.", nameof(Lease));
        if (Status is not (DurableSendStatus.Pending
            or DurableSendStatus.RetryScheduled
            or DurableSendStatus.AwaitingConsumerCompletion))
            throw new ArgumentException("The durable-send status is not claimable.", nameof(Status));

        ArgumentOutOfRangeException.ThrowIfNegative(DeliveryAttempts);
        return this;
    }
}
