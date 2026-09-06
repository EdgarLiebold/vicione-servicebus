using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Selects RabbitMQ fanout exchanges for every message type and entity name.</summary>
public class FanoutExchangeTypeSelector :
    IExchangeTypeSelector
{
    string IExchangeTypeSelector.GetExchangeType<T>(string exchangeName)
    {
        return ExchangeType.Fanout;
    }

    /// <summary>Gets the RabbitMQ fanout exchange type.</summary>
    public string DefaultExchangeType => ExchangeType.Fanout;
}
