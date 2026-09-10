using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adapts consumer-wide or message-wide middleware to one closed consumer-message pipeline.</summary>
/// <typeparam name="TConsumer">The consumer implementation that owns the target pipeline.</typeparam>
/// <typeparam name="TMessage">The message contract delivered through the target pipeline.</typeparam>
public sealed class ConsumerPipeSpecificationProxy<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    readonly IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> _specification;

    /// <summary>Creates an adapter for middleware that surrounds every message handled by the consumer.</summary>
    /// <param name="specification">The consumer-wide middleware specification to project.</param>
    public ConsumerPipeSpecificationProxy(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _specification = new ConsumerSplitFilterSpecification<TConsumer, TMessage>(specification);
    }

    /// <summary>Creates an adapter for middleware that surrounds messages of this contract.</summary>
    /// <param name="specification">The message-level middleware specification to project.</param>
    public ConsumerPipeSpecificationProxy(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _specification = new ConsumerMessageSplitFilterSpecification<TConsumer, TMessage>(specification);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _specification.Apply(builder);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _specification.Validate();
    }
}
