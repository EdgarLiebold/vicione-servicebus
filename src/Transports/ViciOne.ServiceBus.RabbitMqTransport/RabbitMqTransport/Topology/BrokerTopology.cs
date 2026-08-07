// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    public interface BrokerTopology :
        IProbeSite
    {
        Exchange[] Exchanges { get; }
        Queue[] Queues { get; }
        ExchangeToExchangeBinding[] ExchangeBindings { get; }
        ExchangeToQueueBinding[] QueueBindings { get; }
    }
}
