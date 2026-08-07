// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    public interface IMessagePartitionKeyFormatter<in TMessage>
        where TMessage : class
    {
        string FormatPartitionKey(SendContext<TMessage> context);
    }
}
