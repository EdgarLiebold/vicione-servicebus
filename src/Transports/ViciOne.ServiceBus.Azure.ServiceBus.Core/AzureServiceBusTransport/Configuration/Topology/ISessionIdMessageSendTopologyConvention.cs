using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;

public interface ISessionIdMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    void SetFormatter(ISessionIdFormatter formatter);
    void SetFormatter(IMessageSessionIdFormatter<TMessage> formatter);
}
