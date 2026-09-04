using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer split filter specification implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConsumerSplitFilterSpecification<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TMessage : class
    where TConsumer : class
{
    readonly IPipeSpecification<ConsumerConsumeContext<TConsumer>> _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public ConsumerSplitFilterSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _specification = specification;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
    {
        _specification.Apply(new BuilderProxy(builder));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
