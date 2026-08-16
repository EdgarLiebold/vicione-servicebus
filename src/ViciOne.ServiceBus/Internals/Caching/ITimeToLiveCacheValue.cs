namespace ViciOne.ServiceBus.Internals.Caching
{
    public interface ITimeToLiveCacheValue<TValue> :
        ICacheValue<TValue>
        where TValue : class
    {
        long Timestamp { get; }
    }
}
