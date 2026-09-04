using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public class ConsumerSplitFilterSpecification<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TMessage : class
    where TConsumer : class
{
    readonly IPipeSpecification<ConsumerConsumeContext<TConsumer>> _specification;

    public ConsumerSplitFilterSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _specification = specification;
    }

    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
    {
        _specification.Apply(new BuilderProxy(builder));
    }

    public IEnumerable<ValidationResult> Validate()
    {
        if (!typeof(TConsumer).ImplementsInterface<IConsumer<TMessage>>())
            yield return this.Failure("MessageType", $"is not consumed by {TypeCache<TConsumer>.ShortName}");

        foreach (var validationResult in _specification.Validate())
            yield return validationResult;
    }


    class BuilderProxy :
        IPipeBuilder<ConsumerConsumeContext<TConsumer>>
    {
        readonly IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> _builder;

        public BuilderProxy(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
        {
            _builder = builder;
        }

        public void AddFilter(IFilter<ConsumerConsumeContext<TConsumer>> filter)
        {
            _builder.AddFilter(new ConsumerSplitFilter<TConsumer, TMessage>(filter));
        }
    }
}
