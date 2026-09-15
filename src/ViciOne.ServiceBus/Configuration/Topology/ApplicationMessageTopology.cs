using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures application-wide message-contract conventions before the first bus topology is created.
/// Bus- and endpoint-specific runtime policy belongs on their individual configurators.
/// </summary>
public static class ApplicationMessageTopology
{
    /// <summary>Excludes a message contract from automatically generated consume bindings.</summary>
    /// <typeparam name="T">The message contract to exclude.</typeparam>
    public static void ExcludeFromConsumeTopology<T>()
    {
        GlobalTopology.MarkMessageTypeNotConsumable(typeof(T));
    }

    /// <summary>Stops publish topology changes from being copied into send topology.</summary>
    public static void SeparatePublishFromSendConventions()
    {
        GlobalTopology.SeparatePublishFromSend();
    }
}
