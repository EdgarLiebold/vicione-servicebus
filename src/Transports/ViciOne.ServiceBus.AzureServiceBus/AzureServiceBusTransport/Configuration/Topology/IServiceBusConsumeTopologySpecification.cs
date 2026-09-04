using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for service bus consume topology specification.
/// </summary>
public interface IServiceBusConsumeTopologySpecification :
    ISpecification
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
