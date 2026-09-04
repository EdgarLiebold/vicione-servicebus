using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public class PublishMessageSchedulerPipeSpecification :
    IPipeSpecification<ConsumeContext>
{
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new PublishMessageSchedulerFilter());
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
