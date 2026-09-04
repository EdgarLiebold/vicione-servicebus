using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer pipe specification proxy implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConsumerPipeSpecificationProxy<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    readonly IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>> _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public ConsumerPipeSpecificationProxy(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specification = new ConsumerSplitFilterSpecification<TConsumer, TMessage>(specification);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public ConsumerPipeSpecificationProxy(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specification = new ConsumerMessageSplitFilterSpecification<TConsumer, TMessage>(specification);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
    {
        _specification.Apply(builder);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _specification.Validate();
    }
}
