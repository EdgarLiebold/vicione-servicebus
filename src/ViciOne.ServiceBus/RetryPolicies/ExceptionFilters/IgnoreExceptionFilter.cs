using System;
using System.Linq;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

/// <summary>Processes ignore exception pipeline stages.</summary>
public class IgnoreExceptionFilter :
    IExceptionFilter
{
    readonly Type[] _exceptionTypes;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="exceptionTypes">The exception types.</param>
    public IgnoreExceptionFilter(params Type[] exceptionTypes)
    {
        _exceptionTypes = exceptionTypes;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("except");
        scope.Set(new { ExceptionTypes = _exceptionTypes.Select(x => x.Name).ToArray() });
    }

    bool IExceptionFilter.Match(Exception exception)
    {
        for (var i = 0; i < _exceptionTypes.Length; i++)
        {
            if (_exceptionTypes[i].IsInstanceOfType(exception))
                return false;
        }

        return true;
    }
}
