namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Builds the ActiveMQ topic topology required to publish a message.</summary>
public interface IPublishEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the topic to which the message is published.</summary>
    TopicHandle? Topic { get; set; }

    /// <summary>Creates the builder scope used for an implemented message contract.</summary>
    /// <returns>A nested builder when hierarchy is maintained; otherwise, this builder.</returns>
    IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder();
}
