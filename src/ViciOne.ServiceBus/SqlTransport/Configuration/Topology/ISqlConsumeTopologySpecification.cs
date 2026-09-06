using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Describes requirements for sql consume topology.</summary>
public interface ISqlConsumeTopologySpecification :
    ISpecification
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
