namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface ISessionIdMessageSendTopologyConvention<TMessage> :
        IMessageSendTopologyConvention<TMessage>
        where TMessage : class
    {
        void SetFormatter(ISessionIdFormatter formatter);
        void SetFormatter(IMessageSessionIdFormatter<TMessage> formatter);
    }
}
