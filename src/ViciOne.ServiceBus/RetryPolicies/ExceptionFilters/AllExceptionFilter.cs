using System;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

/// <summary>Matches every exception.</summary>
internal sealed class AllExceptionFilter :
    IExceptionFilter
{
    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.CreateScope("all");
    }

    bool IExceptionFilter.Match(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return true;
    }
}
