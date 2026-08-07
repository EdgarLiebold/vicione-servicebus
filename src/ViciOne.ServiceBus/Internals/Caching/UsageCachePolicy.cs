// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.Caching
{
    using System;


    public class UsageCachePolicy<TValue> :
        ICachePolicy<TValue, CacheValue<TValue>>
        where TValue : class
    {
        public CacheValue<TValue> CreateValue(Action remove)
        {
            return new CacheValue<TValue>(remove);
        }

        public bool IsValid(CacheValue<TValue> value)
        {
            return true;
        }

        public int CheckValue(CacheValue<TValue> value)
        {
            return value.Usage;
        }
    }
}
