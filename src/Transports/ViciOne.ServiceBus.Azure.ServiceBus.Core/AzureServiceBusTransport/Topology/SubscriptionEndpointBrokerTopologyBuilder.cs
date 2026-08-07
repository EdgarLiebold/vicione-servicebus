// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    public class SubscriptionEndpointBrokerTopologyBuilder :
        BrokerTopologyBuilder,
        ISubscriptionEndpointBrokerTopologyBuilder
    {
        public TopicHandle Topic { get; set; }
    }
}
