using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

internal static class ReliableMessagingComposition
{
    public static T RequireExactlyOne<T, TBus>(IEnumerable<T> components, string component)
        where T : class
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(components);
        T[] snapshot = components.Take(2).ToArray();
        return snapshot.Length switch
        {
            1 => snapshot[0],
            0 => throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"Durable Sender for bus '{typeof(TBus)}' has no {component}. Configure it inside the owning bus.UseReliableMessaging(...) block.", "Correct the named configuration before starting the host")),
            _ => throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Reliable messaging", "unknown", $"Durable Sender for bus '{typeof(TBus)}' has multiple {component} owners. Configure exactly one.", "Correct the named configuration before starting the host")),
        };
    }
}
