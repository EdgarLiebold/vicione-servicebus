using System;

namespace ViciOne.ServiceBus.Transports;

internal static class SendEndpointCacheDefaults
{
    static SendEndpointCacheDefaults()
    {
        Capacity = 1000;
        MinAge = TimeSpan.FromSeconds(10);
        MaxAge = TimeSpan.FromHours(24);
    }

    public static int Capacity { get; }
    public static TimeSpan MinAge { get; }
    public static TimeSpan MaxAge { get; }
}
