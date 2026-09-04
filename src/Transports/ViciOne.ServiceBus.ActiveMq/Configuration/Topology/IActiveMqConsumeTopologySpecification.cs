using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Defines the contract for active mq consume topology specification.
/// </summary>
public interface IActiveMqConsumeTopologySpecification :
    ISpecification
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
