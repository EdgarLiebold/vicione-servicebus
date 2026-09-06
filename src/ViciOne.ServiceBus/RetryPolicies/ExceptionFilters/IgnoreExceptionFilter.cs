using System;
using System.Linq;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

/// <summary>Handles every exception except the configured exception types.</summary>
public sealed class IgnoreExceptionFilter :
    IExceptionFilter
{
    readonly Type[] _exceptionTypes;

    /// <summary>Creates a filter from the exception types that must not be handled.</summary>
    /// <param name="exceptionTypes">The exception types to exclude.</param>
    public IgnoreExceptionFilter(params Type[] exceptionTypes)
    {
        _exceptionTypes = ValidateAndSnapshot(exceptionTypes);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("except");
        scope.Set(new { ExceptionTypes = _exceptionTypes.Select(x => x.Name).ToArray() });
    }

    bool IExceptionFilter.Match(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (var i = 0; i < _exceptionTypes.Length; i++)
        {
            if (_exceptionTypes[i].IsInstanceOfType(exception))
                return false;
        }

        return true;
    }

    static Type[] ValidateAndSnapshot(Type[] exceptionTypes)
    {
        ArgumentNullException.ThrowIfNull(exceptionTypes);
        if (exceptionTypes.Any(static type => type is null || !typeof(Exception).IsAssignableFrom(type)))
            throw new ArgumentException("Every configured type must derive from Exception.", nameof(exceptionTypes));

        return [.. exceptionTypes];
    }
}
