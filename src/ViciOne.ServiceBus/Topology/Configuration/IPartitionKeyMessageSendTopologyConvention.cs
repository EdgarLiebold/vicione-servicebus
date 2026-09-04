using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public interface IPartitionKeyMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    void SetFormatter(IPartitionKeyFormatter formatter);
    void SetFormatter(IMessagePartitionKeyFormatter<TMessage> formatter);
}
