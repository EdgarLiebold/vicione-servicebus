using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how a request response advances or completes a future.</summary>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TResponse">The response message contract.</typeparam>
public interface IFutureResponseConfigurator<TResult, TResponse> :
    IFutureResultConfigurator<TResult, TResponse>
    where TResult : class
    where TResponse : class
{
    /// <summary>
    /// If specified, the identifier is used to complete a pending result and the result will be stored
    /// in the future.
    /// </summary>
    /// <param name="provider">The provider that extracts the completed operation identifier from the response.</param>
    void CompletePendingRequest(PendingFutureIdProvider<TResponse> provider);

    /// <summary>Adds state-machine activities executed when the response is received.</summary>
    /// <param name="configure">The callback that adds activities to the response event.</param>
    void WhenReceived(Func<IEventActivityBinder<FutureState, TResponse>, IEventActivityBinder<FutureState, TResponse>> configure);
}
