using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs consume topology specification.
/// </summary>
public interface IAmazonSqsConsumeTopologySpecification :
    ISpecification
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
