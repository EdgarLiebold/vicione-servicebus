using System;

namespace ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

/// <summary>Applies a predicate when an exception chain contains the configured exception type.</summary>
/// <typeparam name="T">The exception type inspected by the predicate.</typeparam>
internal sealed class FilterExceptionFilter<T> :
    IExceptionFilter
    where T : Exception
{
    readonly Func<T, bool> _filter;

    /// <summary>Creates an exception filter from a typed predicate.</summary>
    /// <param name="filter">Determines whether a matching exception is handled.</param>
    public FilterExceptionFilter(Func<T, bool> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateScope("filter");
        scope.Set(new { ExceptionType = typeof(T).Name });
    }

    bool IExceptionFilter.Match(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception? currentException = exception;
        while (currentException != null)
        {
            if (currentException is T matchedException)
                return _filter(matchedException);

            currentException = currentException.InnerException;
        }

        return true;
    }
}
