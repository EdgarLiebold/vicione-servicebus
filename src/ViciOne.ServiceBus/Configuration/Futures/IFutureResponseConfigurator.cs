using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures future response.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IFutureResponseConfigurator<TResult, TResponse> :
    IFutureResultConfigurator<TResult, TResponse>
    where TResult : class
    where TResponse : class
{
    /// <summary>
    /// If specified, the identifier is used to complete a pending result and the result will be stored
    /// in the future.
    /// </summary>
    /// <param name="provider">Provides the identifier from the request.</param>
    void CompletePendingRequest(PendingFutureIdProvider<TResponse> provider);

    /// <summary>Add activities to the state machine that are executed when the response is received.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void WhenReceived(Func<EventActivityBinder<FutureState, TResponse>, EventActivityBinder<FutureState, TResponse>> configure);
}
