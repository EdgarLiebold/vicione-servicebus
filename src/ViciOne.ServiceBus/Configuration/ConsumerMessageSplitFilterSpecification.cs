using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Projects message middleware onto one typed consumer-message pipeline.</summary>
/// <typeparam name="TConsumer">The consumer implementation that owns the target pipeline.</typeparam>
/// <typeparam name="TMessage">The message contract exposed to the projected middleware.</typeparam>
public sealed class ConsumerMessageSplitFilterSpecification<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TMessage : class
    where TConsumer : class
{
    readonly IPipeSpecification<ConsumeContext<TMessage>> _specification;

    /// <summary>Creates an adapter for message middleware applied within a consumer pipeline.</summary>
    /// <param name="specification">The message-level middleware specification to project.</param>
    public ConsumerMessageSplitFilterSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
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
        IPipeBuilder<ConsumeContext<TMessage>>
    {
        readonly IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> _builder;

        public BuilderProxy(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void AddFilter(IFilter<ConsumeContext<TMessage>> filter)
        {
            ArgumentNullException.ThrowIfNull(filter);

            _builder.AddFilter(new MessageSplitFilter<TConsumer, TMessage>(filter));
        }
    }
}
