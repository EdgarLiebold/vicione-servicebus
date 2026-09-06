using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Defines a validation-aware mutation of ActiveMQ receive topology.</summary>
public interface IActiveMqConsumeTopologySpecification :
    ISpecification
{
    /// <summary>Applies the specification to a receive-topology builder.</summary>
    /// <param name="builder">The builder to update.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
