namespace ViciOne.ServiceBus.RetryPolicies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    public class IntervalRetryPolicy :
        IRetryPolicy
    {
        readonly IExceptionFilter _filter;

        public IntervalRetryPolicy(IExceptionFilter filter, params TimeSpan[] intervals)
        {
            ArgumentNullException.ThrowIfNull(filter);
            ArgumentNullException.ThrowIfNull(intervals);
            if (intervals.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(intervals), "At least one interval must be specified");
            if (intervals.Any(interval => interval < TimeSpan.Zero))
                throw new ArgumentOutOfRangeException(nameof(intervals), "Retry intervals must be non-negative.");

            _filter = filter;
            Intervals = Array.AsReadOnly([.. intervals]);
        }

        public IntervalRetryPolicy(IExceptionFilter filter, params int[] intervals)
        {
            ArgumentNullException.ThrowIfNull(filter);
            ArgumentNullException.ThrowIfNull(intervals);
            if (intervals.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(intervals), "At least one interval must be specified");
            if (intervals.Any(interval => interval < 0))
                throw new ArgumentOutOfRangeException(nameof(intervals), "Retry intervals must be non-negative.");

            _filter = filter;
            Intervals = Array.AsReadOnly(intervals.Select(x => TimeSpan.FromMilliseconds(x)).ToArray());
        }

        public IReadOnlyList<TimeSpan> Intervals { get; }

        void IProbeSite.Probe(ProbeContext context)
        {
            context.Set(new
            {
                Policy = "Interval",
                Limit = Intervals.Count,
                Intervals
            });

            _filter.Probe(context);
        }

        RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
        {
            return new IntervalRetryPolicyContext<T>(this, context);
        }

        public bool IsHandled(Exception exception)
        {
            return _filter.Match(exception);
        }

        public override string ToString()
        {
            return $"Interval (limit {Intervals.Count}, intervals {string.Join(";", Intervals.Take(5).Select(x => x.ToString()))})";
        }
    }
}
