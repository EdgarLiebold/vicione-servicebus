using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer filter specification implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConsumerFilterSpecification<TConsumer, TMessage> :
    IPipeSpecification<ConsumerConsumeContext<TConsumer, TMessage>>
    where TConsumer : class
    where TMessage : class
{
    readonly IFilter<ConsumerConsumeContext<TConsumer, TMessage>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public ConsumerFilterSpecification(IFilter<ConsumerConsumeContext<TConsumer>> filter)
    {
        _filter = new ConsumerSplitFilter<TConsumer, TMessage>(filter);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumerConsumeContext<TConsumer, TMessage>> builder)
    {
        builder.AddFilter(_filter);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_filter == null)
            yield return this.Failure("Filter", "must not be null");
    }
}
