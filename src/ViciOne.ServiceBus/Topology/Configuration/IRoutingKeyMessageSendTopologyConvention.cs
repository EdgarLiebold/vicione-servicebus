namespace ViciOne.ServiceBus.Configuration
{
    using Transports;


    public interface IRoutingKeyMessageSendTopologyConvention<TMessage> :
        IMessageSendTopologyConvention<TMessage>
        where TMessage : class
    {
        void SetFormatter(IRoutingKeyFormatter formatter);
        void SetFormatter(IMessageRoutingKeyFormatter<TMessage> formatter);
    }
}
