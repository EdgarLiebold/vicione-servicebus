namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for send transform.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISendTransformSpecification<TMessage> :
    IPipeSpecification<SendContext<TMessage>>
    where TMessage : class
{
}
