using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq publish topology.
/// </summary>
public interface IRabbitMqPublishTopology :
    IPublishTopology
{
    /// <summary>
    /// Gets the exchange type selector value.
    /// </summary>
    IExchangeTypeSelector ExchangeTypeSelector { get; }

    /// <summary>
    /// Determines how type hierarchy is configured on the broker
    /// </summary>
    PublishBrokerTopologyOptions BrokerTopologyOptions { get; }

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IRabbitMqMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets publish broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    BrokerTopology GetPublishBrokerTopology();
}
