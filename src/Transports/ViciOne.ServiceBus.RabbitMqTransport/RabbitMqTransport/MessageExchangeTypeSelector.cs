// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using Topology;


    public class MessageExchangeTypeSelector<TMessage> :
        IMessageExchangeTypeSelector<TMessage>
        where TMessage : class
    {
        readonly IExchangeTypeSelector _exchangeTypeSelector;

        public MessageExchangeTypeSelector(IExchangeTypeSelector exchangeTypeSelector)
        {
            _exchangeTypeSelector = exchangeTypeSelector;
        }

        public string DefaultExchangeType => _exchangeTypeSelector.DefaultExchangeType;

        public string GetExchangeType(string exchangeName)
        {
            return _exchangeTypeSelector.GetExchangeType<TMessage>(exchangeName);
        }
    }
}
