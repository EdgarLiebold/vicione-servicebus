using System.Collections.Generic;
using ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public class ServiceBusMessageSchedulerSpecification :
    IPipeSpecification<ConsumeContext>
{
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new ServiceBusMessageSchedulerFilter());
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
