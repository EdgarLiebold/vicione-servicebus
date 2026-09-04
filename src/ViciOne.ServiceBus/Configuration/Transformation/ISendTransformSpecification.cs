namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for send transform specification.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISendTransformSpecification<TMessage> :
    IPipeSpecification<SendContext<TMessage>>
    where TMessage : class
{
}
