using System;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

/// <summary>Processes filter exception pipeline stages.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class FilterExceptionFilter<T> :
    IExceptionFilter
    where T : Exception
{
    readonly Func<T, bool> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public FilterExceptionFilter(Func<T, bool> filter)
    {
        _filter = filter;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("filter");
        scope.Set(new { ExceptionType = typeof(T).Name });
    }

    bool IExceptionFilter.Match(Exception exception)
    {
        var currentException = exception;
        while (currentException != null)
        {
            if (exception is T ex)
                return _filter(ex);

            currentException = currentException.GetBaseException();
        }

        return true;
    }
}
