using System;

namespace ViciOne.ServiceBus.SignalR.Utils;

/// <summary>Tracks service-bus SignalR group memberships for a connection.</summary>
public class ViciOneServiceBusFeature : IViciOneServiceBusFeature
{
    /// <summary>Gets the groups.</summary>
    public ConcurrentHashSet<string> Groups { get; } = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
}
