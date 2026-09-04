using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Defines the contract for rabbit mq consume topology specification.
/// </summary>
public interface IRabbitMqConsumeTopologySpecification :
    ISpecification
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
