using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for scheduled redelivery pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ScheduledRedeliveryPipeSpecification<TMessage> :
    IPipeSpecification<ConsumeContext<TMessage>>,
    IRedeliveryPipeSpecification
    where TMessage : class
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        builder.AddFilter(new ScheduleMessageRedeliveryFilter<TMessage>(Options));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Gets or sets the options.</summary>
    public RedeliveryOptions Options { get; set; } = RedeliveryOptions.ReplaceMessageId;
}
