using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the receive endpoint stopping event data.</summary>
public class ReceiveEndpointStoppingEvent :
    ReceiveEndpointStopping
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="inputAddress">The input address.</param>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    /// <param name="removed">The removed.</param>
    public ReceiveEndpointStoppingEvent(Uri inputAddress, IReceiveEndpoint receiveEndpoint, bool removed)
    {
        InputAddress = inputAddress;
        ReceiveEndpoint = receiveEndpoint;
        Removed = removed;
    }

    /// <summary>Gets the removed.</summary>
    public bool Removed { get; }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the receive endpoint.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
