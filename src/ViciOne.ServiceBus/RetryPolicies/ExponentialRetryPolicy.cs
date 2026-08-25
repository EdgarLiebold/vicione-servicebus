namespace ViciOne.ServiceBus.RetryPolicies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    public class ExponentialRetryPolicy :
        IRetryPolicy
    {
        readonly IExceptionFilter _filter;
        readonly int _highInterval;
        readonly TimeSpan[] _intervals;
        readonly int _lowInterval;
        readonly int _maxInterval;
        readonly int _minInterval;

        public ExponentialRetryPolicy(IExceptionFilter filter, int retryLimit, TimeSpan minInterval, TimeSpan maxInterval, TimeSpan intervalDelta)
        {
            ArgumentNullException.ThrowIfNull(filter);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryLimit);
            if (minInterval < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(minInterval), "The minimum interval must be non-negative.");
            if (maxInterval < minInterval)
                throw new ArgumentOutOfRangeException(nameof(maxInterval), "The maximum interval must not be less than the minimum interval.");
            if (intervalDelta <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(intervalDelta), "The interval delta must be positive.");
            if (maxInterval.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maxInterval), "The maximum interval must not exceed Int32.MaxValue milliseconds.");
            if (intervalDelta.TotalMilliseconds > int.MaxValue / 1.2)
                throw new ArgumentOutOfRangeException(nameof(intervalDelta), "The interval delta is too large.");

            _filter = filter;
            RetryLimit = retryLimit;
            _minInterval = (int)minInterval.TotalMilliseconds;
            _maxInterval = (int)maxInterval.TotalMilliseconds;

            _lowInterval = (int)(intervalDelta.TotalMilliseconds * 0.8);
            _highInterval = (int)(intervalDelta.TotalMilliseconds * 1.2);

            _intervals = CalculateIntervals().ToArray();
        }

        public int RetryLimit { get; }

        void IProbeSite.Probe(ProbeContext context)
        {
            context.Set(new
            {
                Policy = "Exponential",
                Limit = RetryLimit,
                Min = _minInterval,
                Max = _maxInterval,
                Low = _lowInterval,
                High = _highInterval
            });

            _filter.Probe(context);
        }

        RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
        {
            return new ExponentialRetryPolicyContext<T>(this, context);
        }

        public bool IsHandled(Exception exception)
        {
            return _filter.Match(exception);
        }

        public TimeSpan GetRetryInterval(int retryCount)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(retryCount);

            var interval = retryCount < _intervals.Length ? _intervals[retryCount] : _intervals[_intervals.Length - 1];
            var jitter = Random.Shared.NextDouble() * 0.5 + 0.75;
            var milliseconds = Math.Clamp(interval.TotalMilliseconds * jitter, _minInterval, _maxInterval);

            return TimeSpan.FromMilliseconds(milliseconds);
        }

        IEnumerable<TimeSpan> CalculateIntervals()
        {
            var delta = -1;

            for (var i = 0; i < RetryLimit && delta < _maxInterval; i++)
            {
                delta = (int)Math.Min(_minInterval + Math.Pow(2, i) * Random.Shared.Next(_lowInterval, _highInterval), _maxInterval);

                yield return TimeSpan.FromMilliseconds(delta);
            }
        }

        public override string ToString()
        {
            return $"Exponential (limit {RetryLimit}, min {_minInterval}ms, max {_maxInterval}ms)";
        }
    }
}
