using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the receive endpoint completed event data.</summary>
public class ReceiveEndpointCompletedEvent :
    ReceiveEndpointCompleted
{
    readonly ReceiveTransportCompleted _completed;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="completed">The completed.</param>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    public ReceiveEndpointCompletedEvent(ReceiveTransportCompleted completed, IReceiveEndpoint receiveEndpoint)
    {
        _completed = completed;
        ReceiveEndpoint = receiveEndpoint;
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress => _completed.InputAddress;
    /// <summary>Gets the delivery count.</summary>
    public long DeliveryCount => _completed.DeliveryCount;
    /// <summary>Gets the concurrent delivery count.</summary>
    public long ConcurrentDeliveryCount => _completed.ConcurrentDeliveryCount;

    /// <summary>Gets the receive endpoint.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
