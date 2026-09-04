namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for message send pipe.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessageSendPipe<in TMessage> :
    IPipe<SendContext<TMessage>>
    where TMessage : class
{
}
