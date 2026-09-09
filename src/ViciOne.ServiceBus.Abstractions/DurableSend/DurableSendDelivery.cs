using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Persisted durable-send record claimed for one delivery attempt.</summary>
public sealed record DurableSendDelivery
{
    /// <summary>Gets the immutable durable-send intent claimed for delivery.</summary>
    public required SerializedDurableSend Message { get; init; }

    /// <summary>Persisted incarnation token. It changes only when a discarded intent is admitted again under the same durable-send id.</summary>
    public required Guid GenerationToken { get; init; }

    /// <summary>Gets the time at which the current durable intent was admitted.</summary>
    public required DateTimeOffset EnqueuedAt { get; init; }

    /// <summary>Gets the number of delivery attempts persisted before this claim.</summary>
    public required int DeliveryAttempts { get; init; }

    /// <summary>Gets the persisted state held by this claim.</summary>
    public required DurableSendStatus Status { get; init; }

    /// <summary>Gets the exclusive lease that fences state transitions made by this delivery attempt.</summary>
    public required DurableSendLease Lease { get; init; }
}
