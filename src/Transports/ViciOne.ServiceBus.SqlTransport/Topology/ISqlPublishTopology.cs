using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by sql publish topology.</summary>
public interface ISqlPublishTopology :
    IPublishTopology
{
    /// <summary>Gets message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message topology.</returns>
    new ISqlMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Gets publish broker topology.</summary>
    /// <returns>The publish broker topology.</returns>
    BrokerTopology GetPublishBrokerTopology();
}
