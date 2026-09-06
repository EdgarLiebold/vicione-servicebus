namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for future result configurator.</summary>
public static class FutureResultConfiguratorExtensions
{
    /// <summary>Sets completed.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public static void SetCompleted<TResult, TInput>(this IFutureResultConfigurator<TResult, TInput> configurator)
        where TResult : class
        where TInput : class, TResult
    {
        configurator.SetCompletedUsingFactory(context => context.Message);
    }
}
