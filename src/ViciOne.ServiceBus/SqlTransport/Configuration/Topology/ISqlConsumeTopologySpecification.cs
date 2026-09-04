using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Defines the contract for sql consume topology specification.
/// </summary>
public interface ISqlConsumeTopologySpecification :
    ISpecification
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
