using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Controls the lifetime of future request.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TFault">The fault type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface FutureRequestHandle<out TCommand, TResult, TFault, TRequest>
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
{
    /// <summary>The Request Faulted event.</summary>
    Event<Fault<TRequest>> Faulted { get; }

    /// <summary>Handle the response type specified, and configure the response behavior.</summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The future response handle produced by the operation.</returns>
    FutureResponseHandle<TCommand, TResult, TFault, TRequest, T> OnResponseReceived<T>(Action<IFutureResponseConfigurator<TResult, T>>? configure = null)
        where T : class;
}
