using System;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Exposes the ActiveMQ publish topic and broker topology for one message type.</summary>
/// <typeparam name="TMessage">The published message type.</typeparam>
public interface IActiveMqMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IActiveMqMessagePublishTopology
    where TMessage : class
{
    /// <summary>Gets the configured publish topic.</summary>
    Topic Topic { get; }

    /// <summary>Creates the ActiveMQ send settings for the specified host.</summary>
    /// <param name="hostAddress">The configured broker address.</param>
    /// <returns>The settings used to address and configure the publish topic.</returns>
    SendSettings GetSendSettings(Uri hostAddress);

    /// <summary>Builds the broker topology required to publish the message type.</summary>
    /// <param name="options">Options controlling implemented-message hierarchy.</param>
    /// <returns>The message's publish broker topology.</returns>
    BrokerTopology GetBrokerTopology(PublishBrokerTopologyOptions options = PublishBrokerTopologyOptions.MaintainHierarchy);
}


/// <summary>Applies untyped ActiveMQ message publish topology to a broker-topology builder.</summary>
public interface IActiveMqMessagePublishTopology
{
    /// <summary>Applies the message's publish topic unless publishing is excluded.</summary>
    /// <param name="builder">The publish-topology builder.</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
