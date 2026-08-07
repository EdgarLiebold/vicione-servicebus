// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public class MessagePartitionKeyFormatter<TMessage> :
        IMessagePartitionKeyFormatter<TMessage>
        where TMessage : class
    {
        readonly IPartitionKeyFormatter _formatter;

        public MessagePartitionKeyFormatter(IPartitionKeyFormatter formatter)
        {
            _formatter = formatter;
        }

        public string FormatPartitionKey(SendContext<TMessage> context)
        {
            return _formatter.FormatPartitionKey(context);
        }
    }
}
