using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus publish topology configurator.
/// </summary>
public interface IServiceBusPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IServiceBusPublishTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IServiceBusMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    new IServiceBusMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
