namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for message publish pipe.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IMessagePublishPipe<in TMessage> :
    IPipe<PublishContext<TMessage>>
    where TMessage : class
{
}
