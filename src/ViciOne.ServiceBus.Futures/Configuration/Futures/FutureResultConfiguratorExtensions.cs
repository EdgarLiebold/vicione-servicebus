namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides result configuration shortcuts for future inputs that already implement the result contract.</summary>
public static class FutureResultConfiguratorExtensions
{
    /// <summary>Completes the future with the received input message.</summary>
    /// <typeparam name="TResult">The successful future result contract.</typeparam>
    /// <typeparam name="TInput">The input contract that implements the result contract.</typeparam>
    /// <param name="configurator">The result configurator.</param>
    public static void SetCompleted<TResult, TInput>(this IFutureResultConfigurator<TResult, TInput> configurator)
        where TResult : class
        where TInput : class, TResult
    {
        configurator.SetCompletedUsingFactory(context => context.Message);
    }
}
