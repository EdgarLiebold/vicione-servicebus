using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.MessageData.Conventions;

/// <summary>Defines the operations required by message data message consume topology convention.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageDataMessageConsumeTopologyConvention<TMessage> :
    IMessageConsumeTopologyConvention<TMessage>
    where TMessage : class
{
}
