namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates default constructor compensate activity instances.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public static class DefaultConstructorCompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>, new()
    where TLog : class
{
    /// <summary>Gets the compensate factory.</summary>
    public static ICompensateActivityFactory<TActivity, TLog> CompensateFactory => ActivityFactoryCache.Factory;


    static class ActivityFactoryCache
    {
        internal static readonly ICompensateActivityFactory<TActivity, TLog> Factory =
            new FactoryMethodCompensateActivityFactory<TActivity, TLog>(_ => new TActivity());
    }
}
