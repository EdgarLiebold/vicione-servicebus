namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public class RabbitMqMessageSendTopology<TMessage> :
        MessageSendTopology<TMessage>,
        IRabbitMqMessageSendTopologyConfigurator<TMessage>
        where TMessage : class
    {
    }
}
