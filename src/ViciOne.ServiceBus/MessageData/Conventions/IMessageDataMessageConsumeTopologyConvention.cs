using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

public interface IMessageDataMessageConsumeTopologyConvention<TMessage> :
    IMessageConsumeTopologyConvention<TMessage>
    where TMessage : class
{
}
