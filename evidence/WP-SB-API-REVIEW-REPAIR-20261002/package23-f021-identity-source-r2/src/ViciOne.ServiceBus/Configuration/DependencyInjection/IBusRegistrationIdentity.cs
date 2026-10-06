using System;

namespace ViciOne.ServiceBus.Configuration;

internal interface IBusRegistrationIdentity
{
    string BusKey { get; }
    string? PersistenceIdentity { get; }
}


internal static class BusRegistrationIdentity
{
    internal static string GetKey(Type busType)
    {
        ArgumentNullException.ThrowIfNull(busType);

        return busType == typeof(IBus)
            ? "default"
            : $"{busType.Assembly.GetName().Name ?? "unknown"}:{busType.FullName ?? busType.Name}";
    }
}
