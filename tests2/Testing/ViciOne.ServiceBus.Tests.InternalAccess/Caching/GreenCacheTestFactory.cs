namespace ViciOne.ServiceBus.Tests.InternalAccess.Caching;

using ViciOne.ServiceBus.Caching;

public static class GreenCacheTestFactory
{
    public static GreenCache<TValue> Create<TValue>(
        CacheSettings settings,
        Action<Action> scheduleCleanup)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(scheduleCleanup);

        return new GreenCache<TValue>(settings, scheduleCleanup);
    }
}
