using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>
/// Provides a host ready event implementation.
/// </summary>
public class HostReadyEvent :
    HostReady
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <param name="receiveEndpoints">The receive endpoints value.</param>
    /// <param name="riders">The riders value.</param>
    public HostReadyEvent(Uri hostAddress, ReceiveEndpointReady[] receiveEndpoints, RiderReady[] riders)
    {
        HostAddress = hostAddress;
        ReceiveEndpoints = receiveEndpoints;
        Riders = riders;
    }

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public Uri HostAddress { get; }

    /// <summary>
    /// Gets the receive endpoints value.
    /// </summary>
    public ReceiveEndpointReady[] ReceiveEndpoints { get; }

    /// <summary>
    /// Gets the riders value.
    /// </summary>
    public RiderReady[] Riders { get; }
}
