namespace ViciOne.ServiceBus;

public interface IActiveMqMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
