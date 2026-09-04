namespace ViciOne.ServiceBus;

public interface IRabbitMqMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
