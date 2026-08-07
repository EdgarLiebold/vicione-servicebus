// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


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
}
