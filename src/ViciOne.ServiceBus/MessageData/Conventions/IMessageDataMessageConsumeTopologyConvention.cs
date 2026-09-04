using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>
/// Defines the contract for message data message consume topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageDataMessageConsumeTopologyConvention<TMessage> :
    IMessageConsumeTopologyConvention<TMessage>
    where TMessage : class
{
}
