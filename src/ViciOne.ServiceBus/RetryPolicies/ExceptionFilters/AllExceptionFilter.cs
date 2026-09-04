using System;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

/// <summary>
/// Provides an all exception filter implementation.
/// </summary>
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
