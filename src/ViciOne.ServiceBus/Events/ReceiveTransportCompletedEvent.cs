using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the receive transport completed event data.</summary>
public class ReceiveTransportCompletedEvent :
    ReceiveTransportCompleted
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="inputAddress">The input address.</param>
    /// <param name="metrics">The metrics.</param>
    public ReceiveTransportCompletedEvent(Uri inputAddress, DeliveryMetrics metrics)
    {
        InputAddress = inputAddress;
        DeliveryCount = metrics.DeliveryCount;
        ConcurrentDeliveryCount = metrics.ConcurrentDeliveryCount;
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress { get; }
    /// <summary>Gets the delivery count.</summary>
    public long DeliveryCount { get; }
    /// <summary>Gets the concurrent delivery count.</summary>
    public long ConcurrentDeliveryCount { get; }
}
