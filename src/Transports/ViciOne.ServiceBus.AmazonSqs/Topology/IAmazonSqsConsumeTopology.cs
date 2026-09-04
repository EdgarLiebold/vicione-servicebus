using System;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs consume topology.
/// </summary>
public interface IAmazonSqsConsumeTopology :
    IConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IAmazonSqsMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Apply the entire topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);

    /// <summary>
    /// Bind an exchange, using the configurator
    /// </summary>
    /// <param name="topicName"></param>
    /// <param name="configure"></param>
    void Bind(string topicName, Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null);
}
