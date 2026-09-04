using System;

namespace ViciOne.ServiceBus.Configuration;

public class TransformSpecificationConfigurator<TMessage> :
    ITransformSpecificationConfigurator<TMessage>
    where TMessage : class
{
    public IConsumeTransformSpecification<TMessage> Get<T>()
        where T : IConsumeTransformSpecification<TMessage>, new()
    {
        return new T();
    }

    public IConsumeTransformSpecification<TMessage> Get<T>(Func<T> transformFactory)
        where T : IConsumeTransformSpecification<TMessage>
    {
        return transformFactory();
    }
}
