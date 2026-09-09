using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how message contracts are represented by broker publish topology.</summary>
public interface IPublishTopologyConfigurator :
    IPublishTopology,
    ISpecification
{
    /// <summary>Gets the mutable publish topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    new IMessagePublishTopologyConfigurator<TMessage> GetMessageTopology<TMessage>()
        where TMessage : class;

    /// <summary>Gets the mutable publish topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message-specific publish topology.</returns>
    new IMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);

    /// <summary>
    /// Adds a convention that is evaluated when publish topology is created for a message contract.
    /// </summary>
    /// <param name="convention">The publish-topology convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IPublishTopologyConvention convention);

    /// <summary>Adds publish topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="topology">The publish topology to add.</param>
    void AddMessagePublishTopology<TMessage>(IMessagePublishTopology<TMessage> topology)
        where TMessage : class;
}
