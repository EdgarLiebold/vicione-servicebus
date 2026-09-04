namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a default constructor execute activity factory implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public static class DefaultConstructorExecuteActivityFactory<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>, new()
    where TArguments : class
{
    /// <summary>
    /// Gets the execute factory value.
    /// </summary>
    public static IExecuteActivityFactory<TActivity, TArguments> ExecuteFactory => ActivityFactoryCache.Factory;


    static class ActivityFactoryCache
    {
        internal static readonly IExecuteActivityFactory<TActivity, TArguments> Factory =
            new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(_ => new TActivity());
    }
}
