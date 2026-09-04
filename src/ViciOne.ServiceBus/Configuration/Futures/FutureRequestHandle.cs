using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for future request handle.
/// </summary>
/// <typeparam name="TCommand">The t command type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="TFault">The t fault type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
public interface FutureRequestHandle<out TCommand, TResult, TFault, TRequest>
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
{
    /// <summary>
    /// The Request Faulted event
    /// </summary>
    Event<Fault<TRequest>> Faulted { get; }

    /// <summary>
    /// Handle the response type specified, and configure the response behavior
    /// </summary>
    /// <param name="configure"></param>
    /// <typeparam name="T">The response type</typeparam>
    /// <returns></returns>
    FutureResponseHandle<TCommand, TResult, TFault, TRequest, T> OnResponseReceived<T>(Action<IFutureResponseConfigurator<TResult, T>>? configure = null)
        where T : class;
}
