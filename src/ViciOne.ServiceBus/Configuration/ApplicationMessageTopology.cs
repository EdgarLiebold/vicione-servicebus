using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures application-wide message-contract conventions before the first bus topology is created.
/// Bus- and endpoint-specific runtime policy belongs on their individual configurators.
/// </summary>
public static class ApplicationMessageTopology
{
    /// <summary>Excludes from consume topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public static void ExcludeFromConsumeTopology<T>()
    {
        GlobalTopology.MarkMessageTypeNotConsumable(typeof(T));
    }

    /// <summary>Uses independent conventions for publish and send topology.</summary>
    public static void SeparatePublishFromSendConventions()
    {
        GlobalTopology.SeparatePublishFromSend();
    }
}
