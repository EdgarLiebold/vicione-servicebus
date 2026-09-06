using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for send transform.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SendTransformSpecification<TMessage> :
    TransformSpecification<TMessage>,
    ISendTransformSpecification<TMessage>
    where TMessage : class
{
    void IPipeSpecification<SendContext<TMessage>>.Apply(IPipeBuilder<SendContext<TMessage>> builder)
    {
        IMessageInitializer<TMessage> initializer = Build();

        builder.AddFilter(new TransformFilter<TMessage>(initializer));
    }
}
