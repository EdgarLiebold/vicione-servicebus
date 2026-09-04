namespace ViciOne.ServiceBus;

public interface ISqlMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
