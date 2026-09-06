using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the topology for bus.</summary>
public abstract class BusTopology :
    IBusTopology
{
    readonly IHostConfiguration _hostConfiguration;
    readonly ITopologyConfiguration _topologyConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="topologyConfiguration">The topology configuration.</param>
    protected BusTopology(IHostConfiguration hostConfiguration, ITopologyConfiguration topologyConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    /// <summary>Gets the publish topology.</summary>
    public IPublishTopology PublishTopology => _topologyConfiguration.Publish;
    /// <summary>Gets the send topology.</summary>
    public ISendTopology SendTopology => _topologyConfiguration.Send;

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message publish topology produced by the operation.</returns>
    public IMessagePublishTopology<T> Publish<T>()
        where T : class
    {
        return _topologyConfiguration.Publish.GetMessageTopology<T>();
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message send topology produced by the operation.</returns>
    public IMessageSendTopology<T> Send<T>()
        where T : class
    {
        return _topologyConfiguration.Send.GetMessageTopology<T>();
    }

    /// <summary>Applies the message configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology produced by the operation.</returns>
    public IMessageTopology<T> Message<T>()
        where T : class
    {
        return _topologyConfiguration.Message.GetMessageTopology<T>();
    }

    /// <summary>Attempts to get publish address.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="publishAddress">Receives the publish address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPublishAddress(Type messageType, [NotNullWhen(true)] out Uri? publishAddress)
    {
        return _topologyConfiguration.Publish.TryGetPublishAddress(messageType, _hostConfiguration.HostAddress, out publishAddress);
    }

    /// <summary>Attempts to get publish address.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="publishAddress">Receives the publish address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPublishAddress<T>([NotNullWhen(true)] out Uri? publishAddress)
        where T : class
    {
        return _topologyConfiguration.Publish.GetMessageTopology<T>().TryGetPublishAddress(_hostConfiguration.HostAddress, out publishAddress);
    }
}
