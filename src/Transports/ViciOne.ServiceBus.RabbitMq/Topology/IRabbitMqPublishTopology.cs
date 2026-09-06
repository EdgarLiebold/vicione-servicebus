using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Exposes RabbitMQ publish topology and its broker declarations.</summary>
public interface IRabbitMqPublishTopology :
    IPublishTopology
{
    /// <summary>Gets the selector used to determine exchange types for published message contracts.</summary>
    IExchangeTypeSelector ExchangeTypeSelector { get; }

    /// <summary>Gets how implemented-message exchange hierarchies are represented.</summary>
    PublishBrokerTopologyOptions BrokerTopologyOptions { get; }

    /// <summary>Gets publish topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract type.</typeparam>
    /// <returns>The RabbitMQ publish topology for <typeparamref name="T"/>.</returns>
    new IRabbitMqMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Builds the combined broker topology for all configured publish contracts.</summary>
    /// <returns>The de-duplicated publish broker topology.</returns>
    BrokerTopology GetPublishBrokerTopology();
}
