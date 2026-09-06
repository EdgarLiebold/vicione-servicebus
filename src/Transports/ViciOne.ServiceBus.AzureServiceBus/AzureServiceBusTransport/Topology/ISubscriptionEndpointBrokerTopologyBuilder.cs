namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Builds the topic and subscription topology for one Azure Service Bus subscription endpoint.</summary>
public interface ISubscriptionEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the subscribed topic handle.</summary>
    TopicHandle Topic { get; }
}
