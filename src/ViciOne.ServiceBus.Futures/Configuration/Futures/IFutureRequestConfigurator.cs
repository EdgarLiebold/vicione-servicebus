using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a request issued by a future in response to an input message.</summary>
/// <typeparam name="TFault">The future fault contract.</typeparam>
/// <typeparam name="TInput">The input message contract.</typeparam>
/// <typeparam name="TRequest">The request message contract.</typeparam>
public interface IFutureRequestConfigurator<TFault, out TInput, TRequest>
    where TFault : class
    where TInput : class
    where TRequest : class
{
    /// <summary>Sets the request destination; when omitted, the request is published.</summary>
    Uri RequestAddress { set; }

    /// <summary>Sets a context-dependent request destination provider.</summary>
    /// <param name="provider">The provider that selects a destination from the input context.</param>
    void SetRequestAddressProvider(RequestAddressProvider<TInput> provider);

    /// <summary>Creates the request with a synchronous message factory.</summary>
    /// <param name="factoryMethod">The factory that creates the request from the input context.</param>
    void UsingRequestFactory(EventMessageFactory<FutureState, TInput, TRequest> factoryMethod);

    /// <summary>Creates the request with an asynchronous message factory.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the request from the input context.</param>
    void UsingRequestFactory(AsyncEventMessageFactory<FutureState, TInput, TRequest> factoryMethod);

    /// <summary>Initializes the request from the initiating command and additional property values.</summary>
    /// <param name="valueProvider">The provider of additional request property values.</param>
    void UsingRequestInitializer(InitializerValueProvider<TInput> valueProvider);

    /// <summary>
    /// If specified, the request is added to the pending results, using the identifier returned by the
    /// provider. A subsequent result with a matching identifier will complete the pending result.
    /// </summary>
    /// <param name="provider">The provider that extracts the pending-operation identifier from the request.</param>
    void TrackPendingRequest(PendingFutureIdProvider<TRequest> provider);

    /// <summary>Configures how a request fault produces the terminal future fault.</summary>
    /// <param name="configure">The callback that configures the future fault message.</param>
    void OnRequestFaulted(Action<IFutureFaultConfigurator<TFault, Fault<TRequest>>> configure);

    /// <summary>Adds state-machine activities executed when the request faults.</summary>
    /// <param name="configure">The callback that adds activities to the request-fault event.</param>
    void WhenFaulted(Func<EventActivityBinder<FutureState, Fault<TRequest>>, EventActivityBinder<FutureState, Fault<TRequest>>> configure);
}
