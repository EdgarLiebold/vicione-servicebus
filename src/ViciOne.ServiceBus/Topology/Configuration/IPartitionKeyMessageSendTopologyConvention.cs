// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using Transports;


    public interface IPartitionKeyMessageSendTopologyConvention<TMessage> :
        IMessageSendTopologyConvention<TMessage>
        where TMessage : class
    {
        void SetFormatter(IPartitionKeyFormatter formatter);
        void SetFormatter(IMessagePartitionKeyFormatter<TMessage> formatter);
    }
}
