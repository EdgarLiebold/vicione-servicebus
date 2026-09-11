using System;

namespace ViciOne.ServiceBus.Events.Receiving;

/// <summary>Reports that a receive endpoint is stopping or being removed.</summary>
internal sealed class ReceiveEndpointStoppingEvent :
    ReceiveEndpointStopping
{
    /// <summary>Creates a receive-endpoint stopping notification.</summary>
    /// <param name="inputAddress">The endpoint's input address.</param>
    /// <param name="receiveEndpoint">The endpoint that is stopping.</param>
    /// <param name="removed">Whether the endpoint is being removed from its host.</param>
    public ReceiveEndpointStoppingEvent(Uri inputAddress, IReceiveEndpoint receiveEndpoint, bool removed)
    {
        InputAddress = inputAddress ?? throw new ArgumentNullException(nameof(inputAddress));
        ReceiveEndpoint = receiveEndpoint ?? throw new ArgumentNullException(nameof(receiveEndpoint));
        Removed = removed;
    }

    /// <summary>Gets whether the endpoint is being removed from its host.</summary>
    public bool Removed { get; }

    /// <summary>Gets the endpoint's input address.</summary>
    public Uri InputAddress { get; }

    /// <summary>Gets the endpoint that is stopping.</summary>
    public IReceiveEndpoint ReceiveEndpoint { get; }
}
