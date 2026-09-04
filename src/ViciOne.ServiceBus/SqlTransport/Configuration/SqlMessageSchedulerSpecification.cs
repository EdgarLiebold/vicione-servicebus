using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Middleware;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql message scheduler specification implementation.
/// </summary>
public class SqlMessageSchedulerSpecification :
    IPipeSpecification<ConsumeContext>
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new SqlMessageSchedulerFilter());
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
