namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for future result configurator.
/// </summary>
public static class FutureResultConfiguratorExtensions
{
    /// <summary>
    /// Sets completed.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    public static void SetCompleted<TResult, TInput>(this IFutureResultConfigurator<TResult, TInput> configurator)
        where TResult : class
        where TInput : class, TResult
    {
        configurator.SetCompletedUsingFactory(context => context.Message);
    }
}
