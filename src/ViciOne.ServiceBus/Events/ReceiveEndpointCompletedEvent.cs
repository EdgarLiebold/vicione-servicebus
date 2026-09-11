using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Projects a transport-completion snapshot through its owning receive endpoint.</summary>
internal sealed class ReceiveEndpointCompletedEvent :
    ReceiveEndpointCompleted
{
    readonly ReceiveTransportCompleted _completed;

    /// <summary>Creates an endpoint-completion notification.</summary>
    /// <param name="completed">The completed transport and its final delivery counters.</param>
    /// <param name="receiveEndpoint">The endpoint that owns the transport.</param>
    public ReceiveEndpointCompletedEvent(ReceiveTransportCompleted completed, IReceiveEndpoint receiveEndpoint)
    {
        _completed = completed ?? throw new ArgumentNullException(nameof(completed));
        ReceiveEndpoint = receiveEndpoint ?? throw new ArgumentNullException(nameof(receiveEndpoint));
    }

    /// <summary>Gets the completed transport's input address.</summary>
    public Uri InputAddress => _completed.InputAddress;
    /// <summary>Gets the completed transport's delivery count.</summary>
    public long DeliveryCount => _completed.DeliveryCount;
    /// <summary>Gets the completed transport's peak concurrent delivery count.</summary>
    public int MaxConcurrentDeliveryCount => _completed.MaxConcurrentDeliveryCount;

    /// <summary>Gets the endpoint that owns the completed transport.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
