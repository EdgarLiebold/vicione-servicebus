namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consume transform specification.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IConsumeTransformSpecification<TMessage> :
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
}
