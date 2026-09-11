using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Events;

/// <summary>Captures the final delivery metrics reported by a stopped receive transport.</summary>
internal sealed class ReceiveTransportCompletedEvent :
    ReceiveTransportCompleted
{
    /// <summary>Creates a transport-completion snapshot.</summary>
    /// <param name="inputAddress">The address of the completed receive transport.</param>
    /// <param name="metrics">The transport's final delivery counters.</param>
    public ReceiveTransportCompletedEvent(Uri inputAddress, IDeliveryMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        InputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
        DeliveryCount = metrics.DeliveryCount;
        MaxConcurrentDeliveryCount = metrics.MaxConcurrentDeliveryCount;
    }

    /// <summary>Gets the address of the completed receive transport.</summary>
    public Uri InputAddress { get; }
    /// <summary>Gets the number of deliveries accepted by the transport.</summary>
    public long DeliveryCount { get; }
    /// <summary>Gets the highest number of deliveries observed concurrently.</summary>
    public int MaxConcurrentDeliveryCount { get; }
}
