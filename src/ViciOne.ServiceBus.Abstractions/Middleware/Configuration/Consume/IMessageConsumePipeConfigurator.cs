namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures the Consuming of a message type, allowing filters to be applied
/// on Consume.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageConsumePipeConfigurator<TMessage> :
    IPipeConfigurator<ConsumeContext<TMessage>>
    where TMessage : class
{
}
