using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Exposes a transport's message, send, and publish topology from one host configuration.</summary>
public abstract class BusTopology :
    IBusTopology
{
    readonly IHostConfiguration _hostConfiguration;
    readonly ITopologyConfiguration _topologyConfiguration;

    /// <summary>Initializes the topology from its host and topology configurations.</summary>
    /// <param name="hostConfiguration">The configuration that supplies the transport host address.</param>
    /// <param name="topologyConfiguration">The configuration that owns message, send, and publish topology.</param>
    protected BusTopology(IHostConfiguration hostConfiguration, ITopologyConfiguration topologyConfiguration)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        _topologyConfiguration = topologyConfiguration ?? throw new ArgumentNullException(nameof(topologyConfiguration));
    }

    /// <summary>Gets the transport's publish topology.</summary>
    public IPublishTopology PublishTopology => _topologyConfiguration.Publish
        ?? throw new InvalidOperationException("The topology configuration returned no publish topology.");
    /// <summary>Gets the transport's send topology.</summary>
    public ISendTopology SendTopology => _topologyConfiguration.Send
        ?? throw new InvalidOperationException("The topology configuration returned no send topology.");

    /// <summary>Gets the publish topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    public IMessagePublishTopology<T> Publish<T>()
        where T : class
    {
        return PublishTopology.GetMessageTopology<T>()
            ?? throw new InvalidOperationException($"The publish topology returned no topology for '{TypeCache<T>.ShortName}'.");
    }

    /// <summary>Gets the send topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The message-specific send topology.</returns>
    public IMessageSendTopology<T> Send<T>()
        where T : class
    {
        return SendTopology.GetMessageTopology<T>()
            ?? throw new InvalidOperationException($"The send topology returned no topology for '{TypeCache<T>.ShortName}'.");
    }

    /// <summary>Gets the shared topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The message-specific topology.</returns>
    public IMessageTopology<T> Message<T>()
        where T : class
    {
        IMessageTopology messageTopology = _topologyConfiguration.Message
            ?? throw new InvalidOperationException("The topology configuration returned no message topology.");
        return messageTopology.GetMessageTopology<T>()
            ?? throw new InvalidOperationException($"The message topology returned no topology for '{TypeCache<T>.ShortName}'.");
    }

    /// <summary>Attempts to resolve the publish address for a runtime message type.</summary>
    /// <param name="messageType">The message contract type.</param>
    /// <param name="publishAddress">The resolved publish address when available.</param>
    /// <returns><see langword="true" /> when an address is available; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPublishAddress(Type messageType, [NotNullWhen(true)] out Uri? publishAddress)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        bool found = PublishTopology.TryGetPublishAddress(messageType, _hostConfiguration.HostAddress, out publishAddress);
        if (found && publishAddress is null)
            throw new InvalidOperationException($"The publish topology reported an address for '{messageType}' without returning one.");

        return found;
    }

    /// <summary>Attempts to resolve the publish address for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="publishAddress">The resolved publish address when available.</param>
    /// <returns><see langword="true" /> when an address is available; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPublishAddress<T>([NotNullWhen(true)] out Uri? publishAddress)
        where T : class
    {
        bool found = Publish<T>().TryGetPublishAddress(_hostConfiguration.HostAddress, out publishAddress);
        if (found && publishAddress is null)
            throw new InvalidOperationException($"The publish topology reported an address for '{TypeCache<T>.ShortName}' without returning one.");

        return found;
    }
}
