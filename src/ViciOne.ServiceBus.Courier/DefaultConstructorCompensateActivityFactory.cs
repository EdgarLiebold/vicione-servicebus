namespace ViciOne.ServiceBus.Courier;

/// <summary>Provides a cached factory for compensation activities with a parameterless constructor.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public static class DefaultConstructorCompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>, new()
    where TLog : class
{
    /// <summary>Gets the cached compensation activity factory.</summary>
    public static ICompensateActivityFactory<TActivity, TLog> CompensateFactory => ActivityFactoryCache.Factory;


    static class ActivityFactoryCache
    {
        internal static readonly ICompensateActivityFactory<TActivity, TLog> Factory =
            new FactoryMethodCompensateActivityFactory<TActivity, TLog>(_ => new TActivity());
    }
}
