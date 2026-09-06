using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes the events and response configuration for a request issued by a future.</summary>
/// <typeparam name="TCommand">The command that created the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TFault">The future fault contract.</typeparam>
/// <typeparam name="TRequest">The request contract.</typeparam>
public interface FutureRequestHandle<out TCommand, TResult, TFault, TRequest>
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
{
    /// <summary>Gets the event raised when the request faults.</summary>
    Event<Fault<TRequest>> Faulted { get; }

    /// <summary>Adds an accepted response contract and optionally configures its future behavior.</summary>
    /// <typeparam name="T">The accepted response contract.</typeparam>
    /// <param name="configure">The optional callback that configures response processing.</param>
    /// <returns>A handle for the configured response.</returns>
    FutureResponseHandle<TCommand, TResult, TFault, TRequest, T> OnResponseReceived<T>(Action<IFutureResponseConfigurator<TResult, T>>? configure = null)
        where T : class;
}
