// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Caching
{
    public interface IConnectCacheValueObserver<TValue>
        where TValue : class
    {
        ConnectHandle Connect(ICacheValueObserver<TValue> observer);
    }
}
