// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using RabbitMqTransport.Topology;


    public interface IRabbitMqPublishTopology :
        IPublishTopology
    {
        IExchangeTypeSelector ExchangeTypeSelector { get; }

        /// <summary>
        /// Determines how type hierarchy is configured on the broker
        /// </summary>
        PublishBrokerTopologyOptions BrokerTopologyOptions { get; }

        new IRabbitMqMessagePublishTopology<T> GetMessageTopology<T>()
            where T : class;

        BrokerTopology GetPublishBrokerTopology();
    }
}
