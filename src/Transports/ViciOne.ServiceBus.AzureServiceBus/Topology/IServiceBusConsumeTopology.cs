using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus consume topology.
/// </summary>
public interface IServiceBusConsumeTopology :
    IConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IServiceBusMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Apply the entire topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
