namespace ViciOne.ServiceBus.AzureServiceBusTransport.Topology
{
    using ViciOne.ServiceBus.Topology;


    public class ServiceBusMessageSendTopology<TMessage> :
        MessageSendTopology<TMessage>,
        IServiceBusMessageSendTopologyConfigurator<TMessage>
        where TMessage : class
    {
    }
}
