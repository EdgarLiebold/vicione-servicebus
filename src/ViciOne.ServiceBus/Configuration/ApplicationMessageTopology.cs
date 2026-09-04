using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures application-wide message-contract conventions before the first bus topology is created.
/// Bus- and endpoint-specific runtime policy belongs on their individual configurators.
/// </summary>
public static class ApplicationMessageTopology
{
    /// <summary>
    /// Performs the exclude from consume topology operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public static void ExcludeFromConsumeTopology<T>()
    {
        GlobalTopology.MarkMessageTypeNotConsumable(typeof(T));
    }

    /// <summary>
    /// Performs the separate publish from send conventions operation.
    /// </summary>
    public static void SeparatePublishFromSendConventions()
    {
        GlobalTopology.SeparatePublishFromSend();
    }
}
