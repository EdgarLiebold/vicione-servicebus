namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Builds the topic and optional hierarchy subscriptions required to publish a message contract.</summary>
public interface IPublishEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets or sets the topic to which the current message contract is published.</summary>
    TopicHandle? Topic { get; set; }

    /// <summary>Creates a child builder for an implemented message contract.</summary>
    /// <returns>A hierarchy-aware child builder, or this builder when implemented contracts are flattened.</returns>
    IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder();
}
