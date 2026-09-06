namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Represents RabbitMQ send topology for one message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
public interface IRabbitMqMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
