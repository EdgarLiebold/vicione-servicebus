// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport.Topology
{
    public class ReceiveEndpointBrokerTopologyBuilder :
        BrokerTopologyBuilder,
        IReceiveEndpointBrokerTopologyBuilder
    {
        public ReceiveEndpointBrokerTopologyBuilder(ReceiveSettings settings)
        {
            Queue = CreateQueue(settings.QueueName, settings.AutoDeleteOnIdle, settings.MaxDeliveryCount);
        }

        public QueueHandle Queue { get; }
    }
}
