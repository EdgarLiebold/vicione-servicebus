using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures Azure Service Bus publish topics for a bus.</summary>
public interface IServiceBusPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IServiceBusPublishTopology
{
    /// <summary>Gets the publish topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    new IServiceBusMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Gets the publish topology for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The runtime-typed publish topology.</returns>
    new IServiceBusMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
