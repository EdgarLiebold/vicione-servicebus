using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how message contracts are bound to receive endpoints.</summary>
public interface IConsumeTopologyConfigurator :
    IConsumeTopology,
    ISpecification
{
    /// <summary>Gets the mutable consume topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message-specific consume topology.</returns>
    new IMessageConsumeTopologyConfigurator<TMessage> GetMessageTopology<TMessage>()
        where TMessage : class;

    /// <summary>Gets the mutable consume topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message-specific consume topology.</returns>
    IMessageConsumeTopologyConfigurator GetMessageTopology(Type messageType);

    /// <summary>
    /// Adds a convention that is evaluated when consume topology is created for a message contract.
    /// </summary>
    /// <param name="convention">The consume-topology convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IConsumeTopologyConvention convention);
}
