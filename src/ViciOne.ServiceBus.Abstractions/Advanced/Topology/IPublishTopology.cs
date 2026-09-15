using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Provides message-specific topology and address resolution for publication.</summary>
public interface IPublishTopology :
    IPublishTopologyConfigurationObserverConnector
{
    /// <summary>Gets the publish topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    IMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Gets the publish topology for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The message-specific publish topology.</returns>
    IMessagePublishTopology GetMessageTopology(Type messageType);

    /// <summary>
    /// Resolves the publish address before a transport-specific publish context exists.
    /// </summary>
    /// <param name="messageType">The runtime message contract.</param>
    /// <param name="baseAddress">The host base address used to construct the broker entity address.</param>
    /// <param name="publishAddress">The resolved publish address when available.</param>
    /// <returns><see langword="true" /> when an address is available; otherwise, <see langword="false" />.</returns>
    bool TryGetPublishAddress(Type messageType, Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress);
}
