using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a receive transport completed event implementation.
/// </summary>
public class ReceiveTransportCompletedEvent :
    ReceiveTransportCompleted
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="metrics">The metrics value.</param>
    public ReceiveTransportCompletedEvent(Uri inputAddress, DeliveryMetrics metrics)
    {
        InputAddress = inputAddress;
        DeliveryCount = metrics.DeliveryCount;
        ConcurrentDeliveryCount = metrics.ConcurrentDeliveryCount;
    }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress { get; }
    /// <summary>
    /// Gets the delivery count value.
    /// </summary>
    public long DeliveryCount { get; }
    /// <summary>
    /// Gets the concurrent delivery count value.
    /// </summary>
    public long ConcurrentDeliveryCount { get; }
}
