using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consumer filter.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumerFilterSpecification<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    readonly IFilter<ConsumerConsumeContext<TConsumer, TMessage>> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public ConsumerFilterSpecification(IFilter<ConsumerConsumeContext<TConsumer>> filter)
    {
        _filter = new ConsumerSplitFilter<TConsumer, TMessage>(filter);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
    {
        builder.AddFilter(_filter);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_filter == null)
            yield return this.Failure("Filter", "must not be null");
    }
}
