using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures consume topology.</summary>
public interface IConsumeTopologyConfigurator :
    IConsumeTopology,
    ISpecification
{
    /// <summary>Returns the specification for the message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <returns>The message topology.</returns>
    new IMessageConsumeTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Returns the specification for the message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message topology.</returns>
    IMessageConsumeTopologyConfigurator GetMessageTopology(Type messageType);

    /// <summary>
    /// Adds a convention to the topology, which will be applied to every message type
    /// requested, to determine if a convention for the message type is available.
    /// </summary>
    /// <param name="convention">The Consume topology convention.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryAddConvention(IConsumeTopologyConvention convention);
}
