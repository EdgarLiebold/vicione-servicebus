namespace ViciOne.ServiceBus.Courier;

/// <summary>Provides a cached factory for execution activities with a parameterless constructor.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public static class DefaultConstructorExecuteActivityFactory<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>, new()
    where TArguments : class
{
    /// <summary>Gets the cached execution activity factory.</summary>
    public static IExecuteActivityFactory<TActivity, TArguments> ExecuteFactory => ActivityFactoryCache.Factory;


    static class ActivityFactoryCache
    {
        internal static readonly IExecuteActivityFactory<TActivity, TArguments> Factory =
            new FactoryMethodExecuteActivityFactory<TActivity, TArguments>(_ => new TActivity());
    }
}
