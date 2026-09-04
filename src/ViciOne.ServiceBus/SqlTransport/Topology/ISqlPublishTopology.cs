using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql publish topology.
/// </summary>
public interface ISqlPublishTopology :
    IPublishTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new ISqlMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets publish broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    BrokerTopology GetPublishBrokerTopology();
}
