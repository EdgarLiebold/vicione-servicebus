// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport.Topology
{
    public interface IActiveMqTemporaryQueueNameFormatter
    {
        public string Format(string queueName);
    }
}
