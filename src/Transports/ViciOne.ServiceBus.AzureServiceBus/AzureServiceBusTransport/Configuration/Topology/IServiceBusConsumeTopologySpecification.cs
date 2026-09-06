using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Applies and validates one Azure Service Bus consume-topology operation.</summary>
public interface IServiceBusConsumeTopologySpecification :
    ISpecification
{
    /// <summary>Applies the operation to a receive-endpoint broker-topology builder.</summary>
    /// <param name="builder">The builder that receives the topology operation.</param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
