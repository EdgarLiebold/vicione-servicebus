using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql publish topology configurator.
/// </summary>
public interface ISqlPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    ISqlPublishTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new ISqlMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    new ISqlMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
