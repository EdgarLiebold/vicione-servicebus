using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga filter specification implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SagaFilterSpecification<TSaga, TMessage> :
    IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly IFilter<SagaConsumeContext<TSaga>> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public SagaFilterSpecification(IFilter<SagaConsumeContext<TSaga>> filter)
    {
        _filter = filter;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
    {
        builder.AddFilter(new SagaSplitFilter<TSaga, TMessage>(_filter));
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
