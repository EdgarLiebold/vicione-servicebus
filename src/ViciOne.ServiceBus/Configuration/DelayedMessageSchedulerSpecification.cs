using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public class DelayedMessageSchedulerSpecification :
    IPipeSpecification<ConsumeContext>
{
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new DelayedMessageSchedulerFilter());
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
