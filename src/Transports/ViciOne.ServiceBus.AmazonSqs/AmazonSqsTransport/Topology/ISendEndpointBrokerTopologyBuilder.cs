namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Builds Amazon SQS topology for a queue send endpoint.</summary>
public interface ISendEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the Amazon SQS queue to which messages are sent.</summary>
    QueueHandle? Queue { get; }
}
