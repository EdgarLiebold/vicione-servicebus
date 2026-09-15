using System;

namespace ViciOne.ServiceBus.ExceptionFilters;

sealed class CompositeExceptionFilter :
    IExceptionFilter
{
    readonly CompositeFilter<Exception> _filter;

    public CompositeExceptionFilter(CompositeFilter<Exception> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("filter", "composite");
    }

    public bool Match(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return _filter.Matches(exception);
    }
}
