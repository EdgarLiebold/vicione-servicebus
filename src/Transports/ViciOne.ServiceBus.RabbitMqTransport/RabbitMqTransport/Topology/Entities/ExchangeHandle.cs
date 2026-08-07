// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public interface ExchangeHandle :
        EntityHandle
    {
        Exchange Exchange { get; }
    }
}
