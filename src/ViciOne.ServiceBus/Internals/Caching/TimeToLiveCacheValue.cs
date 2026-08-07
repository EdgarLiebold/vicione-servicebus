// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.Caching
{
    using System;


    public class TimeToLiveCacheValue<TValue> :
        CacheValue<TValue>,
        ITimeToLiveCacheValue<TValue>
        where TValue : class
    {
        public TimeToLiveCacheValue(Action remove, long timestamp)
            : base(remove)
        {
            Timestamp = timestamp;
        }

        public long Timestamp { get; set; }
    }
}
