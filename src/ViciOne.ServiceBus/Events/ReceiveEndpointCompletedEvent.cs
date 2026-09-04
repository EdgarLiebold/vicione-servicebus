using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a receive endpoint completed event implementation.
/// </summary>
public class ReceiveEndpointCompletedEvent :
    ReceiveEndpointCompleted
{
    readonly ReceiveTransportCompleted _completed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="completed">The completed value.</param>
    /// <param name="receiveEndpoint">The receive endpoint value.</param>
    public ReceiveEndpointCompletedEvent(ReceiveTransportCompleted completed, IReceiveEndpoint receiveEndpoint)
    {
        _completed = completed;
        ReceiveEndpoint = receiveEndpoint;
    }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress => _completed.InputAddress;
    /// <summary>
    /// Gets the delivery count value.
    /// </summary>
    public long DeliveryCount => _completed.DeliveryCount;
    /// <summary>
    /// Gets the concurrent delivery count value.
    /// </summary>
    public long ConcurrentDeliveryCount => _completed.ConcurrentDeliveryCount;

    /// <summary>
    /// Gets the receive endpoint value.
    /// </summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
