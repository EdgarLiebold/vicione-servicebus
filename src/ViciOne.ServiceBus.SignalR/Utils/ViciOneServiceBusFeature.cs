using System;

namespace ViciOne.ServiceBus.SignalR.Utils;

/// <summary>
/// Provides a vici one service bus feature implementation.
/// </summary>
public class ViciOneServiceBusFeature : IViciOneServiceBusFeature
{
    /// <summary>
    /// Gets the groups value.
    /// </summary>
    public ConcurrentHashSet<string> Groups { get; } = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
}
