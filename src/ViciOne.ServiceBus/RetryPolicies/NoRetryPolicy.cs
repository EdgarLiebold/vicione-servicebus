using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Defines policy for no retry.</summary>
public class NoRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public NoRetryPolicy(IExceptionFilter filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        context.Set(new { Policy = "None" });
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        return new NoRetryPolicyContext<T>(this, context);
    }

    bool IRetryPolicy.IsHandled(Exception exception)
    {
        return _filter.Match(exception);
    }

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return "None";
    }
}
