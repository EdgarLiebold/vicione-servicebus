namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Builds an Amazon SQS receive queue and its Amazon SNS subscription topology.</summary>
public interface IReceiveEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the consuming Amazon SQS queue handle.</summary>
    QueueHandle? Queue { get; }
}
