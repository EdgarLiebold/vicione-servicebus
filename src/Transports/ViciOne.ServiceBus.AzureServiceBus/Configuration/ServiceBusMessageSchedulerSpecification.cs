using System.Collections.Generic;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a service bus message scheduler specification implementation.
/// </summary>
public class ServiceBusMessageSchedulerSpecification :
    IPipeSpecification<ConsumeContext>
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new ServiceBusMessageSchedulerFilter());
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
