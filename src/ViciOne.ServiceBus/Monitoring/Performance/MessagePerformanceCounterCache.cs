// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Monitoring.Performance
{
    using System;


    public static class MessagePerformanceCounterCache<T>
    {
        static Lazy<MessagePerformanceCounter<T>> _cache;

        public static IMessagePerformanceCounter Counter(ICounterFactory factory)
        {
            if (_cache == null)
                _cache = new Lazy<MessagePerformanceCounter<T>>(() => new MessagePerformanceCounter<T>(factory));
            return _cache.Value;
        }
    }
}
