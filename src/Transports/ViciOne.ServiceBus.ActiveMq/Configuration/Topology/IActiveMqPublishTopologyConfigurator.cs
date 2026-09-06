using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ virtual-topic publish topology.</summary>
public interface IActiveMqPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IActiveMqPublishTopology
{
    /// <summary>
    /// Sets the prefix prepended to message entity names published through ActiveMQ virtual topics.
    /// The default is <c>VirtualTopic.</c>; an empty value disables the prefix.
    /// </summary>
    new string VirtualTopicPrefix { set; }

    /// <summary>
    /// Sets the regular expression used to recognize named virtual-topic consumer destinations.
    /// Matching destinations retain their configured names instead of using broker-generated temporary names.
    /// </summary>
    /// <seealso href="https://activemq.apache.org/virtual-destinations">Virtual Destinations</seealso>
    new string VirtualTopicConsumerPattern { set; }

    /// <summary>Gets configurable publish topology for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <returns>The ActiveMQ message publish-topology configurator.</returns>
    new IActiveMqMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Gets configurable publish topology for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The untyped ActiveMQ message publish-topology configurator.</returns>
    new IActiveMqMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
