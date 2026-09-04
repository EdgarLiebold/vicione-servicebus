using System;

namespace ViciOne.ServiceBus.SignalR.Utils;

public class ViciOneServiceBusFeature : IViciOneServiceBusFeature
{
    public ConcurrentHashSet<string> Groups { get; } = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
}
