using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

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
