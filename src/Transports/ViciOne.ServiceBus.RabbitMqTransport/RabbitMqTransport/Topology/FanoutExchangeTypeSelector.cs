// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    using RabbitMQ.Client;


    public class FanoutExchangeTypeSelector :
        IExchangeTypeSelector
    {
        string IExchangeTypeSelector.GetExchangeType<T>(string exchangeName)
        {
            return ExchangeType.Fanout;
        }

        public string DefaultExchangeType => ExchangeType.Fanout;
    }
}
