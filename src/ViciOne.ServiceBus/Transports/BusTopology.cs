using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a bus topology implementation.
/// </summary>
public abstract class BusTopology :
    IBusTopology
{
    readonly IHostConfiguration _hostConfiguration;
    readonly ITopologyConfiguration _topologyConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    protected BusTopology(IHostConfiguration hostConfiguration, ITopologyConfiguration topologyConfiguration)
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    public IPublishTopology PublishTopology => _topologyConfiguration.Publish;
    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    public ISendTopology SendTopology => _topologyConfiguration.Send;

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IMessagePublishTopology<T> Publish<T>()
        where T : class
    {
        return _topologyConfiguration.Publish.GetMessageTopology<T>();
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IMessageSendTopology<T> Send<T>()
        where T : class
    {
        return _topologyConfiguration.Send.GetMessageTopology<T>();
    }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IMessageTopology<T> Message<T>()
        where T : class
    {
        return _topologyConfiguration.Message.GetMessageTopology<T>();
    }

    /// <summary>
    /// Attempts to get publish address.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPublishAddress(Type messageType, [NotNullWhen(true)] out Uri? publishAddress)
    {
        return _topologyConfiguration.Publish.TryGetPublishAddress(messageType, _hostConfiguration.HostAddress, out publishAddress);
    }

    /// <summary>
    /// Attempts to get publish address.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="publishAddress">The publish address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public virtual bool TryGetPublishAddress<T>([NotNullWhen(true)] out Uri? publishAddress)
        where T : class
    {
        return _topologyConfiguration.Publish.GetMessageTopology<T>().TryGetPublishAddress(_hostConfiguration.HostAddress, out publishAddress);
    }
}
