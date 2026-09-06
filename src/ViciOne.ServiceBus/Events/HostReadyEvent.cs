using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Carries the host ready event data.</summary>
public class HostReadyEvent :
    HostReady
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <param name="receiveEndpoints">The receive endpoints.</param>
    /// <param name="riders">The riders.</param>
    public HostReadyEvent(Uri hostAddress, ReceiveEndpointReady[] receiveEndpoints, RiderReady[] riders)
    {
        HostAddress = hostAddress;
        ReceiveEndpoints = receiveEndpoints;
        Riders = riders;
    }

    /// <summary>Gets the host address.</summary>
    public Uri HostAddress { get; }

    /// <summary>Gets the receive endpoints.</summary>
    public ReceiveEndpointReady[] ReceiveEndpoints { get; }

    /// <summary>Gets the riders.</summary>
    public RiderReady[] Riders { get; }
}
