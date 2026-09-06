using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures Azure Service Bus send conventions for a bus.</summary>
public interface IServiceBusSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IServiceBusSendTopology
{
    /// <summary>Sets the callback applied when error-queue entity settings are created.</summary>
    Action<IServiceBusEntityConfigurator> ConfigureErrorSettings { set; }
    /// <summary>Sets the callback applied when dead-letter-queue entity settings are created.</summary>
    Action<IServiceBusEntityConfigurator> ConfigureDeadLetterSettings { set; }

    /// <summary>Gets the send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <returns>The message-specific send topology.</returns>
    new IServiceBusMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
