using System;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

/// <summary>Processes all exception pipeline stages.</summary>
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
