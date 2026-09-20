using System;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

internal static class BrokerName
{
    public static bool Equals(string? left, string? right) =>
        StringComparer.OrdinalIgnoreCase.Equals(left, right);

    public static int GetHashCode(string value) =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(value);
}
