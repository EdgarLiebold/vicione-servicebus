namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

public interface IMessageRoutingKeyFormatter<in TMessage>
    where TMessage : class
{
    string FormatRoutingKey(SendContext<TMessage> context);
}
