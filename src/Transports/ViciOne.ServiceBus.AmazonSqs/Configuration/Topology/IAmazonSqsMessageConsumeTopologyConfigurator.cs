using System;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures the Amazon SNS topic subscription used to consume a message type from Amazon SQS.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IAmazonSqsMessageConsumeTopologyConfigurator<TMessage> :
    IMessageConsumeTopologyConfigurator<TMessage>,
    IAmazonSqsMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Adds the topic subscriptions for this message type.</summary>
    /// <param name="configure">The callback that configures the topic subscription.</param>
    void Subscribe(Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null);
}


/// <summary>Applies untyped Amazon SQS message consume topology to an endpoint builder.</summary>
public interface IAmazonSqsMessageConsumeTopologyConfigurator :
    IMessageConsumeTopologyConfigurator
{
    /// <summary>Applies message-specific subscriptions to a receive-endpoint builder.</summary>
    /// <param name="builder">The receive-endpoint broker-topology builder.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
