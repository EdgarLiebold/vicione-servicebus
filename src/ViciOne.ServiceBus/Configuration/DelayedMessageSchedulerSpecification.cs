using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for delayed message scheduler.</summary>
public class DelayedMessageSchedulerSpecification :
    IPipeSpecification<ConsumeContext>
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new DelayedMessageSchedulerFilter());
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
