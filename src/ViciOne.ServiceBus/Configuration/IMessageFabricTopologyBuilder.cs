using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Configuration;

public interface IMessageFabricTopologyBuilder
{
    void ExchangeBind(string source, string destination, string? routingKey);

    void QueueBind(string source, string destination);

    void ExchangeDeclare(string name, ExchangeType exchangeType);

    void QueueDeclare(string name);
}
