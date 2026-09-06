namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consume transform.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IConsumeTransformSpecification<TMessage> :
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
}
