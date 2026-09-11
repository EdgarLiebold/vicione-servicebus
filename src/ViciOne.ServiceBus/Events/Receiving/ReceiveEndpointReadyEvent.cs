using System;

namespace ViciOne.ServiceBus.Events.Receiving;

/// <summary>Reports that a receive endpoint is available to observers.</summary>
internal sealed class ReceiveEndpointReadyEvent :
    ReceiveEndpointReady
{
    /// <summary>Creates a receive-endpoint readiness notification.</summary>
    /// <param name="inputAddress">The endpoint's input address.</param>
    /// <param name="receiveEndpoint">The endpoint that became available.</param>
    /// <param name="isStarted">Whether this notification follows endpoint startup.</param>
    public ReceiveEndpointReadyEvent(Uri inputAddress, IReceiveEndpoint receiveEndpoint, bool isStarted)
    {
        InputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
        ReceiveEndpoint = receiveEndpoint ?? throw new ArgumentNullException(nameof(receiveEndpoint));
        IsStarted = isStarted;
    }

    /// <summary>Gets the endpoint's input address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the endpoint that became available.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }

    /// <summary>Gets a value indicating whether this notification follows endpoint startup.</summary>
    public bool IsStarted { get; }
}
