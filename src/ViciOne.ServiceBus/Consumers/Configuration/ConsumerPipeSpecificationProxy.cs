using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Forwards consumer pipe specification operations to an underlying context.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumerPipeSpecificationProxy<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    readonly IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="specification">The specification.</param>
    public ConsumerPipeSpecificationProxy(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specification = new ConsumerSplitFilterSpecification<TConsumer, TMessage>(specification);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="specification">The specification.</param>
    public ConsumerPipeSpecificationProxy(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specification = new ConsumerMessageSplitFilterSpecification<TConsumer, TMessage>(specification);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
    {
        _specification.Apply(builder);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _specification.Validate();
    }
}
