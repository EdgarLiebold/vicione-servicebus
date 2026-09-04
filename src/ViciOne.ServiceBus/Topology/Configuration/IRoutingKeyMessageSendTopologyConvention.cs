using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

public interface IRoutingKeyMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    void SetFormatter(IRoutingKeyFormatter formatter);
    void SetFormatter(IMessageRoutingKeyFormatter<TMessage> formatter);
}
