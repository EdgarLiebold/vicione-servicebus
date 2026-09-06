using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures publish topology.</summary>
public interface IPublishTopologyConfigurator :
    IPublishTopology,
    ISpecification
{
    /// <summary>Returns the specification for the message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message topology.</returns>
    new IMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Returns the specification for the message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message topology.</returns>
    new IMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);

    /// <summary>
    /// Adds a convention to the topology, which will be applied to every message type
    /// requested, to determine if a convention for the message type is available.
    /// </summary>
    /// <param name="convention">The Publish topology convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IPublishTopologyConvention convention);

    /// <summary>Add a Publish topology for a specific message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="topology">The topology.</param>
    void AddMessagePublishTopology<T>(IMessagePublishTopology<T> topology)
        where T : class;
}
