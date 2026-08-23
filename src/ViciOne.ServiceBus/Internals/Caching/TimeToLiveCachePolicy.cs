namespace ViciOne.ServiceBus.Internals.Caching
{
    using System;


    public class TimeToLiveCachePolicy<TValue> :
        ICachePolicy<TValue, ITimeToLiveCacheValue<TValue>>
        where TValue : class
    {
        readonly TimeProvider _timeProvider;
        readonly TimeSpan _timeToLive;

        public TimeToLiveCachePolicy(TimeSpan timeToLive)
            : this(timeToLive, TimeProvider.System)
        {
        }

        public TimeToLiveCachePolicy(TimeSpan timeToLive, TimeProvider timeProvider)
        {
            if (timeToLive < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeToLive), "Time to live must not be negative");

            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _timeToLive = timeToLive;
        }

        public ITimeToLiveCacheValue<TValue> CreateValue(Action remove)
        {
            return new TimeToLiveCacheValue<TValue>(remove, _timeProvider.GetTimestamp());
        }

        public bool IsValid(ITimeToLiveCacheValue<TValue> value)
        {
            return _timeProvider.GetElapsedTime(value.Timestamp) <= _timeToLive;
        }

        public int CheckValue(ITimeToLiveCacheValue<TValue> value)
        {
            var usage = value.Usage;

            return IsValid(value)
                ? usage
                : int.MinValue;
        }
    }
}
