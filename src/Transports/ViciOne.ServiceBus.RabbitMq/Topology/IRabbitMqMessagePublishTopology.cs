using System;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq message publish topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IRabbitMqMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IRabbitMqMessagePublishTopology
    where TMessage : class
{
    /// <summary>
    /// Gets the exchange value.
    /// </summary>
    Exchange Exchange { get; }

    /// <summary>
    /// Returns the send settings for a publish endpoint, which are mostly unused now with topology
    /// </summary>
    /// <param name="hostAddress"></param>
    /// <returns></returns>
    SendSettings GetSendSettings(Uri hostAddress);

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    BrokerTopology GetBrokerTopology();
}


/// <summary>
/// Defines the contract for rabbit mq message publish topology.
/// </summary>
public interface IRabbitMqMessagePublishTopology
{
    /// <summary>
    /// Apply the message topology to the builder, including any implemented types
    /// </summary>
    /// <param name="builder">The topology builder</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
