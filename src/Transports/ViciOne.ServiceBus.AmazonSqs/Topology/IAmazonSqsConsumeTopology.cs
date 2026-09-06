using System;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Builds Amazon SNS topic subscriptions for an Amazon SQS receive queue.</summary>
public interface IAmazonSqsConsumeTopology :
    IConsumeTopology
{
    /// <summary>Gets consume topology for a message type.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <returns>The typed message consume topology.</returns>
    new IAmazonSqsMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Applies all configured subscriptions to a receive-endpoint builder.</summary>
    /// <param name="builder">The receive-endpoint broker-topology builder.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);

    /// <summary>Subscribes the receive queue to a topic using the supplied configurator.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">An optional callback that configures the subscription.</param>
    void Bind(string topicName, Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null);
}
