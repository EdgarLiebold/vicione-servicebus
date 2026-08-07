// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ExceptionFilters
{
    using System;


    class CompositeExceptionFilter :
        IExceptionFilter
    {
        readonly CompositeFilter<Exception> _filter;

        public CompositeExceptionFilter(CompositeFilter<Exception> filter)
        {
            _filter = filter;
        }

        public void Probe(ProbeContext context)
        {
            context.Add("filter", "composite");
        }

        public bool Match(Exception exception)
        {
            return _filter.Matches(exception);
        }
    }
}
