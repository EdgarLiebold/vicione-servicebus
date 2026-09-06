using System;
using ViciOne.ServiceBus.RabbitMq;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Describes the RabbitMQ exchange topology used to publish one message contract.</summary>
/// <typeparam name="TMessage">The published message contract type.</typeparam>
public interface IRabbitMqMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IRabbitMqMessagePublishTopology
    where TMessage : class
{
    /// <summary>Gets the message contract's exchange declaration.</summary>
    Exchange Exchange { get; }

    /// <summary>Creates the RabbitMQ send settings for the specified host.</summary>
    /// <param name="hostAddress">The RabbitMQ host address.</param>
    /// <returns>The settings used to address and configure the publish exchange.</returns>
    SendSettings GetSendSettings(Uri hostAddress);

    /// <summary>Builds the complete broker topology required to publish the message contract.</summary>
    /// <returns>The publish broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}


/// <summary>Applies a message contract's RabbitMQ publish topology to a broker-topology builder.</summary>
public interface IRabbitMqMessagePublishTopology
{
    /// <summary>Applies the message exchange and directly implemented contract topology.</summary>
    /// <param name="builder">The publish-endpoint topology builder.</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
