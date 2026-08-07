// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.Caching
{
    public interface ITimeToLiveCacheValue<TValue> :
        ICacheValue<TValue>
        where TValue : class
    {
        long Timestamp { get; }
    }
}
