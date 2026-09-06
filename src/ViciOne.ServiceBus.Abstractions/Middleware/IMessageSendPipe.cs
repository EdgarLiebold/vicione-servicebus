namespace ViciOne.ServiceBus.Middleware;

/// <summary>Defines the operations required by message send pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageSendPipe<in TMessage> :
    IPipe<SendContext<TMessage>>
    where TMessage : class
{
}
