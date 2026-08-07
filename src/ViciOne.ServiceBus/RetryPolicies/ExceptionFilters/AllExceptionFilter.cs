// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters
{
    using System;


    public class AllExceptionFilter :
        IExceptionFilter
    {
        void IProbeSite.Probe(ProbeContext context)
        {
            context.CreateScope("all");
        }

        bool IExceptionFilter.Match(Exception exception)
        {
            return true;
        }
    }
}
