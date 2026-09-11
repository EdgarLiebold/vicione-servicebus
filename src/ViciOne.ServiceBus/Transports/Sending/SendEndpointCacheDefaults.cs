using System;

namespace ViciOne.ServiceBus.Transports;

internal static class SendEndpointCacheDefaults
{
    public const int Capacity = 1000;
    public static TimeSpan MinAge { get; } = TimeSpan.FromSeconds(10);
    public static TimeSpan MaxAge { get; } = TimeSpan.FromHours(24);
}
