using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Identifies a send-topology convention owned by message-data transformation.</summary>
/// <typeparam name="TMessage">The message contract inspected by the convention.</typeparam>
internal interface IMessageDataMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
}
