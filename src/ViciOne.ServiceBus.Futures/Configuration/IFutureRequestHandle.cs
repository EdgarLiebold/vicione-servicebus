using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes response and fault configuration for a request issued by a future.</summary>
/// <typeparam name="TCommand">The command that created the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TFault">The future fault contract.</typeparam>
/// <typeparam name="TRequest">The request contract.</typeparam>
public interface IFutureRequestHandle<out TCommand, TResult, TFault, TRequest>
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
{
    /// <summary>Gets the event raised when the request faults.</summary>
    IEvent<Fault<TRequest>> Faulted { get; }

    /// <summary>Adds an accepted response contract and configures how it advances the future.</summary>
    /// <typeparam name="T">The accepted response contract.</typeparam>
    /// <param name="configure">The callback that configures response processing.</param>
    /// <returns>A handle for the configured response.</returns>
    IFutureResponseHandle<TCommand, TResult, TFault, TRequest, T> OnResponseReceived<T>(Action<IFutureResponseConfigurator<TResult, T>> configure)
        where T : class;
}
