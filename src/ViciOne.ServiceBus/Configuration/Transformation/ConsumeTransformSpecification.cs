using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consume transform specification implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConsumeTransformSpecification<TMessage> :
    TransformSpecification<TMessage>,
    IConsumeTransformSpecification<TMessage>
    where TMessage : class
{
    void IPipeSpecification<ConsumeContext<TMessage>>.Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        IMessageInitializer<TMessage> initializer = Build();

        builder.AddFilter(new TransformFilter<TMessage>(initializer));
    }
}
