using System.Collections.Generic;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds the Azure Service Bus scheduler payload to each consume context.</summary>
public class ServiceBusMessageSchedulerSpecification :
    IPipeSpecification<ConsumeContext>
{
    /// <summary>Adds the scheduler filter to the consume pipeline.</summary>
    /// <param name="builder">The consume-pipeline builder.</param>
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new ServiceBusMessageSchedulerFilter());
    }

    /// <summary>Reports configuration validation failures.</summary>
    /// <returns>An empty sequence because this specification has no configurable state.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
