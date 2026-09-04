using System;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

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
