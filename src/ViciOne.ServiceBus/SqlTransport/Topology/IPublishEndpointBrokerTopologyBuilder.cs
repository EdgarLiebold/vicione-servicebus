namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>A builder for creating the topology when publishing a message.</summary>
public interface IPublishEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the topic to which the message is published.</summary>
    TopicHandle? Topic { get; set; }

    /// <summary>Creates implemented builder.</summary>
    /// <returns>The created implemented builder.</returns>
    IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder();
}
