// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AmazonSqsTransport.Topology;

/// <summary>
/// A builder for creating the topology when publishing a message
/// </summary>
public interface IPublishEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>
    /// The exchange to which the message is published
    /// </summary>
    TopicHandle? Topic { get; set; }
}
