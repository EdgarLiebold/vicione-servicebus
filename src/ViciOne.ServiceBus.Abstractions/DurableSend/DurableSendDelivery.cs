using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Persisted durable-send record claimed for one delivery attempt.</summary>
public sealed record DurableSendDelivery
{
    /// <summary>
    /// Gets or sets the message value.
    /// </summary>
    public required SerializedDurableSend Message { get; init; }

    /// <summary>Persisted incarnation token. It changes only when a discarded intent is admitted again under the same durable-send id.</summary>
    public required Guid GenerationToken { get; init; }

    /// <summary>
    /// Gets or sets the enqueued at value.
    /// </summary>
    public required DateTimeOffset EnqueuedAt { get; init; }

    /// <summary>
    /// Gets or sets the delivery attempts value.
    /// </summary>
    public required int DeliveryAttempts { get; init; }

    /// <summary>
    /// Gets or sets the status value.
    /// </summary>
    public required DurableSendStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the lease value.
    /// </summary>
    public required DurableSendLease Lease { get; init; }
}
