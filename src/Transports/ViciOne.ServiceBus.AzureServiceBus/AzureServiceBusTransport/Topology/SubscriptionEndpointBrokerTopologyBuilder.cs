namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a subscription endpoint broker topology builder implementation.
/// </summary>
public class SubscriptionEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    ISubscriptionEndpointBrokerTopologyBuilder
{
    /// <summary>
    /// Gets or sets the topic value.
    /// </summary>
    public TopicHandle Topic { get; set; } = null!;
}
