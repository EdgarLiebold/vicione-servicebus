namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a default constructor compensate activity factory implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public static class DefaultConstructorCompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>, new()
    where TLog : class
{
    /// <summary>
    /// Gets the compensate factory value.
    /// </summary>
    public static ICompensateActivityFactory<TActivity, TLog> CompensateFactory => ActivityFactoryCache.Factory;


    static class ActivityFactoryCache
    {
        internal static readonly ICompensateActivityFactory<TActivity, TLog> Factory =
            new FactoryMethodCompensateActivityFactory<TActivity, TLog>(_ => new TActivity());
    }
}
