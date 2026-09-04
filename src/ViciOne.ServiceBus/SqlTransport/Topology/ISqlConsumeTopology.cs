using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql consume topology.
/// </summary>
public interface ISqlConsumeTopology :
    IConsumeTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new ISqlMessageConsumeTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Apply the entire topology to the builder
    /// </summary>
    /// <param name="builder"></param>
    void Apply(IReceiveEndpointBrokerTopologyBuilder builder);
}
