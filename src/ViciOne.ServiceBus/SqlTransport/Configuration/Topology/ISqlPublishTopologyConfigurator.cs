using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql publish topology.</summary>
public interface ISqlPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    ISqlPublishTopology
{
    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new ISqlMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Gets message topology.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message topology.</returns>
    new ISqlMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
