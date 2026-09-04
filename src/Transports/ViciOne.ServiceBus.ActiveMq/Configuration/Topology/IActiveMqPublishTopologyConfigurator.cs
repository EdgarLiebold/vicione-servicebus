using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq publish topology configurator.
/// </summary>
public interface IActiveMqPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IActiveMqPublishTopology
{
    /// <summary>
    /// ActiveMQ supports publish/subscribe using virtual topics. By default, virtual topics
    /// must be prefixed with "VirtualTopic." however those defaults can be changed. Changing
    /// this setting will use the specified prefix instead (or "", if so specified).
    /// </summary>
    new string VirtualTopicPrefix { set; }

    /// <summary>
    /// Regular expression to distinguish if a destination is for consuming data from a VirtualTopic.
    /// Because bind is on server side and rely on names Virtual topics and connected consumers cannot be
    /// created as temporary. A temporary destinations does not support custom names than we must use regular destinations.
    /// </summary>
    /// <seealso href="https://activemq.apache.org/virtual-destinations">Virtual Destinations</seealso>
    new string VirtualTopicConsumerPattern { set; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IActiveMqMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    new IActiveMqMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
