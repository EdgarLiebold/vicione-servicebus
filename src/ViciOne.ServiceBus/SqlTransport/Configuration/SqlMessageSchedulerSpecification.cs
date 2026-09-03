namespace ViciOne.ServiceBus.SqlTransport.Configuration
{
    using System.Collections.Generic;
    using ViciOne.ServiceBus.Configuration;
    using Middleware;


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
}
