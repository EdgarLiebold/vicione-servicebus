using System;

namespace ViciOne.ServiceBus.Events;

/// <summary>Captures the receive endpoints and riders that became ready with a bus host.</summary>
internal sealed class HostReadyEvent :
    HostReady
{
    /// <summary>Creates an immutable host-readiness snapshot.</summary>
    /// <param name="hostAddress">The ready host's transport address.</param>
    /// <param name="receiveEndpoints">The endpoints that reached readiness.</param>
    /// <param name="riders">The riders that reached readiness.</param>
    public HostReadyEvent(Uri hostAddress, ReceiveEndpointReady[] receiveEndpoints, RiderReady[] riders)
    {
        HostAddress = hostAddress ?? throw new ArgumentNullException(nameof(hostAddress));
        ReceiveEndpoints = [.. receiveEndpoints ?? throw new ArgumentNullException(nameof(receiveEndpoints))];
        Riders = [.. riders ?? throw new ArgumentNullException(nameof(riders))];
    }

    /// <summary>Gets the ready host's transport address.</summary>
    public Uri HostAddress { get; }

    /// <summary>Gets a snapshot of endpoints that reached readiness.</summary>
    public ReceiveEndpointReady[] ReceiveEndpoints { get; }

    /// <summary>Gets a snapshot of riders that reached readiness.</summary>
    public RiderReady[] Riders { get; }
}
