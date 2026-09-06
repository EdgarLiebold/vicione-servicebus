using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the receive endpoint ready event data.</summary>
public class ReceiveEndpointReadyEvent :
    ReceiveEndpointReady
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="inputAddress">The input address.</param>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    /// <param name="isStarted">The is started.</param>
    public ReceiveEndpointReadyEvent(Uri inputAddress, IReceiveEndpoint receiveEndpoint, bool isStarted)
    {
        InputAddress = inputAddress;
        ReceiveEndpoint = receiveEndpoint;
        IsStarted = isStarted;
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the receive endpoint.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }

    /// <summary>Gets a value indicating whether started.</summary>
    public bool IsStarted { get; }
}
