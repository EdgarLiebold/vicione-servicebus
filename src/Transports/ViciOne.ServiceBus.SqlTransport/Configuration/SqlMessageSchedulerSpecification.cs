using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Middleware;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Describes requirements for sql message scheduler.</summary>
public class SqlMessageSchedulerSpecification :
    IPipeSpecification<ConsumeContext>
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new SqlMessageSchedulerFilter());
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
