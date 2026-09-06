namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Builds topology for an Azure Service Bus topic subscription endpoint.</summary>
public class SubscriptionEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    ISubscriptionEndpointBrokerTopologyBuilder
{
    /// <summary>Gets or sets the subscribed topic handle.</summary>
    public TopicHandle Topic { get; set; } = null!;
}
