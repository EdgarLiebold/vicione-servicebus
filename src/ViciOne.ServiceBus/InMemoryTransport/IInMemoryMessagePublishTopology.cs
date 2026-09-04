using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus;

public interface IInMemoryMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopology
    where TMessage : class
{
    ExchangeType ExchangeType { get; }
}


public interface IInMemoryMessagePublishTopology
{
    void Apply(IMessageFabricPublishTopologyBuilder builder);
}
