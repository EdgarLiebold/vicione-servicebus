using System;
using System.ComponentModel;

namespace ViciOne.ServiceBus;

/// <summary>Persisted durable-send record claimed for one delivery attempt.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record DurableSendDelivery
{
    public required SerializedDurableSend Message { get; init; }

    /// <summary>Persisted incarnation token. It changes only when a discarded intent is admitted again under the same durable-send id.</summary>
    public required Guid GenerationToken { get; init; }

    public required DateTimeOffset EnqueuedAt { get; init; }

    public required int DeliveryAttempts { get; init; }

    public required DurableSendStatus Status { get; init; }

    public required DurableSendLease Lease { get; init; }
}
