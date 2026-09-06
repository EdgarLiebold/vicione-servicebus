using System;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs message publish topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IAmazonSqsMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IAmazonSqsMessagePublishTopology
    where TMessage : class
{
    /// <summary>
    /// Gets the topic value.
    /// </summary>
    Topic Topic { get; }

    /// <summary>
    /// Creates the Amazon SNS publish settings for the specified host.
    /// </summary>
    /// <returns>The settings used to address and configure the publish endpoint.</returns>
    PublishSettings GetPublishSettings(Uri hostAddress);

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    BrokerTopology GetBrokerTopology();
}


/// <summary>
/// Defines the contract for amazon sqs message publish topology.
/// </summary>
public interface IAmazonSqsMessagePublishTopology
{
    /// <summary>
    /// Apply the message topology to the builder, including any implemented types
    /// </summary>
    /// <param name="builder">The topology builder</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
