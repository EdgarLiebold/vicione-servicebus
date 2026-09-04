using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a delayed redelivery pipe specification implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class DelayedRedeliveryPipeSpecification<TMessage> :
    IPipeSpecification<ConsumeContext<TMessage>>,
    IRedeliveryPipeSpecification
    where TMessage : class
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        builder.AddFilter(new DelayedMessageRedeliveryFilter<TMessage>(Options));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Gets or sets the options value.
    /// </summary>
    public RedeliveryOptions Options { get; set; }
}
