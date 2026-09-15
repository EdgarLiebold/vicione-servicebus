using System;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Produces terminal decisions without scheduling retry attempts.</summary>
internal sealed class NoRetryPolicy :
    IRetryPolicy
{
    readonly IExceptionFilter _filter;

    /// <summary>Creates a terminal policy with the supplied exception selection.</summary>
    /// <param name="filter">Determines which terminal failures receive retry-fault notifications.</param>
    public NoRetryPolicy(IExceptionFilter filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Set(new { Policy = "None" });
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new NoRetryPolicyContext<T>(this, context);
    }

    bool IRetryPolicy.IsHandled(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return _filter.Match(exception);
    }

    /// <summary>Returns the diagnostic policy name.</summary>
    /// <returns><c>None</c>.</returns>
    public override string ToString()
    {
        return "None";
    }
}
