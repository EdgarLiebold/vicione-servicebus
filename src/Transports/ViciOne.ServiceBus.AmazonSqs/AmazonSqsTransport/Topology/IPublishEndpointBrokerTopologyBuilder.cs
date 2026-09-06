namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Builds Amazon SNS topology for a publish endpoint.</summary>
public interface IPublishEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the Amazon SNS topic to which the message is published.</summary>
    TopicHandle? Topic { get; set; }
}
