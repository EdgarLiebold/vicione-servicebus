using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Declares and validates part of an Amazon SNS-to-SQS consume topology.</summary>
public interface IAmazonSqsConsumeTopologySpecification :
    ISpecification
{
    /// <summary>Applies the subscription declaration to a receive-endpoint builder.</summary>
    /// <param name="builder">The receive-endpoint broker-topology builder.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
