using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Describes a bus host after its receive endpoints and riders have completed startup.</summary>
public interface HostReady
{
    /// <summary>Gets the host transport address.</summary>
    Uri HostAddress { get; }

    /// <summary>Gets the receive-endpoint readiness snapshots collected for the host.</summary>
    IReadOnlyList<ReceiveEndpointReady> ReceiveEndpoints { get; }

    /// <summary>Gets the rider readiness snapshots collected for the host.</summary>
    IReadOnlyList<RiderReady> Riders { get; }
}
