using System;
using ViciOne.ServiceBus.ActiveMq;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq message publish topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IActiveMqMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IActiveMqMessagePublishTopology
    where TMessage : class
{
    /// <summary>
    /// Gets the topic value.
    /// </summary>
    Topic Topic { get; }

    /// <summary>
    /// Creates the ActiveMQ send settings for the specified host.
    /// </summary>
    /// <param name="hostAddress">The ActiveMQ host address.</param>
    /// <returns>The settings used to address and configure the publish topic.</returns>
    SendSettings GetSendSettings(Uri hostAddress);

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    BrokerTopology GetBrokerTopology(PublishBrokerTopologyOptions options = PublishBrokerTopologyOptions.MaintainHierarchy);
}


/// <summary>
/// Defines the contract for active mq message publish topology.
/// </summary>
public interface IActiveMqMessagePublishTopology
{
    /// <summary>
    /// Apply the message topology to the builder, including any implemented types
    /// </summary>
    /// <param name="builder">The topology builder</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
