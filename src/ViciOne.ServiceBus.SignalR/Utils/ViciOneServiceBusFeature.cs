// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SignalR.Utils
{
    using System;


    public class ViciOneServiceBusFeature : IViciOneServiceBusFeature
    {
        public ConcurrentHashSet<string> Groups { get; } = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
