using System;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Describes the Amazon SNS topic and broker topology used to publish a message type.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IAmazonSqsMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IAmazonSqsMessagePublishTopology
    where TMessage : class
{
    /// <summary>Gets the Amazon SNS topic declaration.</summary>
    Topic Topic { get; }

    /// <summary>Creates the Amazon SNS publish settings for the specified host.</summary>
    /// <param name="hostAddress">The Amazon SQS host address.</param>
    /// <returns>The settings used to address and configure the publish endpoint.</returns>
    PublishSettings GetPublishSettings(Uri hostAddress);

    /// <summary>Builds broker topology for the message topic.</summary>
    /// <returns>The topic broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}


/// <summary>Applies untyped Amazon SNS message publish topology to a broker-topology builder.</summary>
public interface IAmazonSqsMessagePublishTopology
{
    /// <summary>Applies the message topic and any implemented-type topology to a publish builder.</summary>
    /// <param name="builder">The publish-endpoint broker-topology builder.</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
