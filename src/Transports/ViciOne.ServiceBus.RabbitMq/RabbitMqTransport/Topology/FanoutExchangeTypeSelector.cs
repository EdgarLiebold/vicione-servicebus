using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a fanout exchange type selector implementation.
/// </summary>
public class FanoutExchangeTypeSelector :
    IExchangeTypeSelector
{
    string IExchangeTypeSelector.GetExchangeType<T>(string exchangeName)
    {
        return ExchangeType.Fanout;
    }

    /// <summary>
    /// Gets the default exchange type value.
    /// </summary>
    public string DefaultExchangeType => ExchangeType.Fanout;
}
