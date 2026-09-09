using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Events;

/// <summary>Captures the receive endpoints and riders that became ready with a bus host.</summary>
internal sealed class HostReadyEvent :
    HostReady
{
    readonly IReadOnlyList<ReceiveEndpointReady> _receiveEndpoints;
    readonly IReadOnlyList<RiderReady> _riders;

    /// <summary>Creates an immutable host-readiness snapshot.</summary>
    /// <param name="hostAddress">The ready host's transport address.</param>
    /// <param name="receiveEndpoints">The endpoints that reached readiness.</param>
    /// <param name="riders">The riders that reached readiness.</param>
    public HostReadyEvent(
        Uri hostAddress,
        IEnumerable<ReceiveEndpointReady> receiveEndpoints,
        IEnumerable<RiderReady> riders)
    {
        HostAddress = hostAddress ?? throw new ArgumentNullException(nameof(hostAddress));
        ArgumentNullException.ThrowIfNull(receiveEndpoints);
        ArgumentNullException.ThrowIfNull(riders);

        _receiveEndpoints = Array.AsReadOnly(receiveEndpoints.Select(item => item
            ?? throw new ArgumentException("Host readiness cannot contain a null receive endpoint.", nameof(receiveEndpoints))).ToArray());
        _riders = Array.AsReadOnly(riders.Select(item => item
            ?? throw new ArgumentException("Host readiness cannot contain a null rider.", nameof(riders))).ToArray());
    }

    /// <summary>Gets the ready host's transport address.</summary>
    public Uri HostAddress { get; }

    /// <summary>Gets a snapshot of endpoints that reached readiness.</summary>
    public IReadOnlyList<ReceiveEndpointReady> ReceiveEndpoints => _receiveEndpoints;

    /// <summary>Gets a snapshot of riders that reached readiness.</summary>
    public IReadOnlyList<RiderReady> Riders => _riders;
}
