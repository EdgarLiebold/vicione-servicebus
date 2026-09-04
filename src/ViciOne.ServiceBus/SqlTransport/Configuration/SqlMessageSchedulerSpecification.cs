using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Middleware;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public class SqlMessageSchedulerSpecification :
    IPipeSpecification<ConsumeContext>
{
    public void Apply(IPipeBuilder<ConsumeContext> builder)
    {
        builder.AddFilter(new SqlMessageSchedulerFilter());
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }
}
