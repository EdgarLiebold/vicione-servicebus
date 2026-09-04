using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public class FanoutExchangeTypeSelector :
    IExchangeTypeSelector
{
    string IExchangeTypeSelector.GetExchangeType<T>(string exchangeName)
    {
        return ExchangeType.Fanout;
    }

    public string DefaultExchangeType => ExchangeType.Fanout;
}
