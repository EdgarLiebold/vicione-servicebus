namespace ViciOne.ServiceBus.Middleware;

/// <summary>Defines the operations required by message publish pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessagePublishPipe<in TMessage> :
    IPipe<PublishContext<TMessage>>
    where TMessage : class
{
}
