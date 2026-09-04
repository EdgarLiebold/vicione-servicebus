using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>
/// Defines the contract for message data message send topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageDataMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
}
