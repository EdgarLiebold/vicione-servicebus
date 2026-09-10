using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Describes requirements for sql publish topology.</summary>
public interface ISqlPublishTopologySpecification :
    ISpecification
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
