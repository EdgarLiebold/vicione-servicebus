using System;
using System.Linq;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

/// <summary>
/// Provides a handle exception filter implementation.
/// </summary>
public class HandleExceptionFilter :
    IExceptionFilter
{
    readonly Type[] _exceptionTypes;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exceptionTypes">The exception types value.</param>
    public HandleExceptionFilter(params Type[] exceptionTypes)
    {
        _exceptionTypes = exceptionTypes;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("selected");
        scope.Set(new { ExceptionTypes = _exceptionTypes.Select(x => x.Name).ToArray() });
    }

    bool IExceptionFilter.Match(Exception exception)
    {
        for (var i = 0; i < _exceptionTypes.Length; i++)
        {
            if (_exceptionTypes[i].IsInstanceOfType(exception))
                return true;
        }

        return false;
    }
}
