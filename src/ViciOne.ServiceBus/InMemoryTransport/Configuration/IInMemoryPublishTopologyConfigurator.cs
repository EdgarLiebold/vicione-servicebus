using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures in memory publish topology.</summary>
public interface IInMemoryPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IInMemoryPublishTopology
{
    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new IInMemoryMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Gets message topology.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message topology.</returns>
    new IInMemoryMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
