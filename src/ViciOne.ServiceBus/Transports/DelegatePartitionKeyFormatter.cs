// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System;


    public class DelegatePartitionKeyFormatter<TMessage> :
        IMessagePartitionKeyFormatter<TMessage>
        where TMessage : class
    {
        readonly Func<SendContext<TMessage>, string> _formatter;

        public DelegatePartitionKeyFormatter(Func<SendContext<TMessage>, string> formatter)
        {
            _formatter = formatter;
        }

        public string FormatPartitionKey(SendContext<TMessage> context)
        {
            return _formatter(context) ?? "";
        }
    }
}
