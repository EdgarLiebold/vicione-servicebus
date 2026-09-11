using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Identifies a consume-topology convention owned by message-data transformation.</summary>
/// <typeparam name="TMessage">The message contract inspected by the convention.</typeparam>
internal interface IMessageDataMessageConsumeTopologyConvention<TMessage> :
    IMessageConsumeTopologyConvention<TMessage>
    where TMessage : class
{
}
