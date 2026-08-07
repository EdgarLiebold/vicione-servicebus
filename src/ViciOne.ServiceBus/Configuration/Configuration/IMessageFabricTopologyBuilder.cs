// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using Transports.Fabric;


    public interface IMessageFabricTopologyBuilder
    {
        void ExchangeBind(string source, string destination, string routingKey);

        void QueueBind(string source, string destination);

        void ExchangeDeclare(string name, ExchangeType exchangeType);

        void QueueDeclare(string name);
    }
}
