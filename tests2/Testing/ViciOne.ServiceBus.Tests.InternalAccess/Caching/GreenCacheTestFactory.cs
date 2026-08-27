namespace ViciOne.ServiceBus.Tests.InternalAccess.Caching;

using ViciOne.ServiceBus.Caching;

public static class GreenCacheTestFactory
{
    public static GreenCache<TValue> Create<TValue>(
        CacheSettings settings,
        Func<Action, bool> tryScheduleCleanup)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tryScheduleCleanup);

        return new GreenCache<TValue>(settings, tryScheduleCleanup);
    }
}
