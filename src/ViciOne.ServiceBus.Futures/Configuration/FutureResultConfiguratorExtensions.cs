using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides result configuration for inputs that already implement the future result contract.</summary>
public static class FutureResultConfiguratorExtensions
{
    /// <summary>Uses the received input message as the successful future result.</summary>
    /// <typeparam name="TResult">The successful future result contract.</typeparam>
    /// <typeparam name="TInput">The input contract that implements the result contract.</typeparam>
    /// <param name="configurator">The result configurator.</param>
    public static void SetResultFromInput<TResult, TInput>(this IFutureResultConfigurator<TResult, TInput> configurator)
        where TResult : class
        where TInput : class, TResult
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.SetResultFactory(context => context.Message);
    }
}
