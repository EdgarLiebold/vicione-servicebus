namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq message send topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IRabbitMqMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
