using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures message-specific publish topology for the in-memory transport.</summary>
public interface IInMemoryPublishTopologyConfigurator :
    IPublishTopologyConfigurator
{
    /// <summary>Gets mutable publish topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message-specific in-memory publish topology.</returns>
    new IInMemoryMessagePublishTopologyConfigurator<TMessage> GetMessageTopology<TMessage>()
        where TMessage : class;

    /// <summary>Gets mutable publish topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message-specific in-memory publish topology.</returns>
    new IInMemoryMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
