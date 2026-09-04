using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Configuration;

public interface IMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention
    where TMessage : class
{
    bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology);
}


public interface IMessageSendTopologyConvention
{
    bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class;
}
