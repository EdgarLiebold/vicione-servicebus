// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IRabbitMqPublishTopologyConfigurator :
        IPublishTopologyConfigurator,
        IRabbitMqPublishTopology
    {
        /// <summary>
        /// Determines how type hierarchy is configured on the broker
        /// </summary>
        new PublishBrokerTopologyOptions BrokerTopologyOptions { set; }

        new IRabbitMqMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
            where T : class;

        new IRabbitMqMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
    }
}
