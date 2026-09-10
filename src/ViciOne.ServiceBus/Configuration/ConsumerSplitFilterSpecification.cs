using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Projects consumer-wide middleware onto one typed consumer-message pipeline.</summary>
/// <typeparam name="TConsumer">The consumer implementation exposed to the projected middleware.</typeparam>
/// <typeparam name="TMessage">The message contract that closes the target consumer pipeline.</typeparam>
public sealed class ConsumerSplitFilterSpecification<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TMessage : class
    where TConsumer : class
{
    readonly IPipeSpecification<ConsumerConsumeContext<TConsumer>> _specification;

    /// <summary>Creates an adapter for consumer-wide middleware applied to one message contract.</summary>
    /// <param name="specification">The consumer-wide middleware specification to project.</param>
    public ConsumerSplitFilterSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        _specification = specification ?? throw new ArgumentNullException(nameof(specification));
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _specification.Apply(new BuilderProxy(builder));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (!typeof(TConsumer).ImplementsInterface<IConsumer<TMessage>>())
            yield return this.Failure("MessageType", $"is not consumed by {TypeCache<TConsumer>.ShortName}");

        foreach (var validationResult in _specification.Validate())
            yield return validationResult;
    }


    sealed class BuilderProxy :
        IPipeBuilder<ConsumerConsumeContext<TConsumer>>
    {
        readonly IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> _builder;

        public BuilderProxy(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void AddFilter(IFilter<ConsumerConsumeContext<TConsumer>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            _builder.AddFilter(new ConsumerSplitFilter<TConsumer, TMessage>(filter));
        }
    }
}
