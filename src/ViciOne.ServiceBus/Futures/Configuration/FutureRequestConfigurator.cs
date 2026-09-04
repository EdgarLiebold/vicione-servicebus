using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a future request configurator implementation.
/// </summary>
/// <typeparam name="TCommand">The t command type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="TFault">The t fault type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class FutureRequestConfigurator<TCommand, TResult, TFault, TInput, TRequest> :
    FutureRequestHandle<TCommand, TResult, TFault, TRequest>,
    IFutureRequestConfigurator<TFault, TInput, TRequest>,
    ISpecification
    where TCommand : class
    where TResult : class
    where TFault : class
    where TInput : class
    where TRequest : class
{
    readonly IFutureStateMachineConfigurator _configurator;
    readonly FutureFault<TCommand, TFault, Fault<TRequest>> _fault;
    readonly FutureRequest<TInput, TRequest> _request;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="faulted">The faulted value.</param>
    public FutureRequestConfigurator(IFutureStateMachineConfigurator configurator, Event<Fault<TRequest>> faulted)
    {
        _configurator = configurator;

        Faulted = faulted;

        _request = new FutureRequest<TInput, TRequest>();
        _fault = new FutureFault<TCommand, TFault, Fault<TRequest>>();
    }

    /// <summary>
    /// Gets or sets the pending request id provider value.
    /// </summary>
    public PendingFutureIdProvider<TRequest> PendingRequestIdProvider
    {
        get => _request.PendingRequestIdProvider;
        private set => _request.PendingRequestIdProvider = value;
    }

    /// <summary>
    /// Gets the faulted value.
    /// </summary>
    public Event<Fault<TRequest>> Faulted { get; }

    /// <summary>
    /// Performs the on response received operation.
    /// </summary>
    /// <typeparam name="TResponse">The t response type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public FutureResponseHandle<TCommand, TResult, TFault, TRequest, TResponse>
        OnResponseReceived<TResponse>(Action<IFutureResponseConfigurator<TResult, TResponse>>? configure)
        where TResponse : class
    {
        var response = new FutureResponseConfigurator<TCommand, TResult, TFault, TRequest, TResponse>(_configurator, this);

        configure?.Invoke(response);

        Validate().ThrowIfContainsFailure($"The future request response was not configured correctly: {TypeCache.GetShortName(GetType())}");

        if (response.PendingResponseIdProvider != null)
            _configurator.CompletePendingRequest(response.Completed, response.PendingResponseIdProvider);
        else
            _configurator.SetResult(response.Completed, context => response.SetResultAsync(context, context.CancellationToken));

        return response;
    }

    /// <summary>
    /// Gets or sets the request address value.
    /// </summary>
    public Uri RequestAddress
    {
        set { _request.AddressProvider = context => value; }
    }

    /// <summary>
    /// Sets request address provider.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public void SetRequestAddressProvider(RequestAddressProvider<TInput> provider)
    {
        _request.AddressProvider = provider;
    }

    /// <summary>
    /// Performs the using request factory operation.
    /// </summary>
    /// <param name="factoryMethod">The factory method value.</param>
    public void UsingRequestFactory(EventMessageFactory<FutureState, TInput, TRequest> factoryMethod)
    {
        _request.Factory = MessageFactory<TRequest>.Create(factoryMethod);
    }

    /// <summary>
    /// Performs the using request factory operation.
    /// </summary>
    /// <param name="factoryMethod">The factory method value.</param>
    public void UsingRequestFactory(AsyncEventMessageFactory<FutureState, TInput, TRequest> factoryMethod)
    {
        _request.Factory = MessageFactory<TRequest>.Create(factoryMethod);
    }

    /// <summary>
    /// Performs the using request initializer operation.
    /// </summary>
    /// <param name="valueProvider">The value provider value.</param>
    public void UsingRequestInitializer(InitializerValueProvider<TInput> valueProvider)
    {
        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>> FactoryAsync(BehaviorContext<FutureState, TInput> context)
        {
            return MessageInitializerCache<TRequest>.InitializeMessageAsync(context, valueProvider(context), new object[] { context.Message });
        }

        _request.Factory = MessageFactory<TRequest>.Create((Func<BehaviorContext<FutureState, TInput>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>>)FactoryAsync);
    }

    /// <summary>
    /// Performs the track pending request operation.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public void TrackPendingRequest(PendingFutureIdProvider<TRequest> provider)
    {
        PendingRequestIdProvider = provider;
    }

    /// <summary>
    /// Performs the on request faulted operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void OnRequestFaulted(Action<IFutureFaultConfigurator<TFault, Fault<TRequest>>> configure)
    {
        var configurator = new FutureFaultConfigurator<TCommand, TFault, Fault<TRequest>>(_fault);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the when faulted operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void WhenFaulted(Func<EventActivityBinder<FutureState, Fault<TRequest>>, EventActivityBinder<FutureState, Fault<TRequest>>> configure)
    {
        _configurator.DuringAnyWhen(Faulted, configure);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _request.Validate();
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        return context.Message != null
            ? _request.SendRequestAsync(context, cancellationToken: cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="data">The data value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(BehaviorContext<FutureState, TCommand> context, TInput data, CancellationToken cancellationToken = default)
    {
        return data != null
            ? _request.SendRequestAsync(context.CreateProxy(MessageEvent<TInput>.Instance, data), cancellationToken: cancellationToken)
            : Task.CompletedTask;
    }

    /// <summary>
    /// Sends range.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="inputs">The inputs value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendRangeAsync(BehaviorContext<FutureState, TCommand> context, IEnumerable<TInput> inputs, CancellationToken cancellationToken = default)
    {
        return inputs != null
            ? Task.WhenAll(inputs.Select(input => SendAsync(context, input, cancellationToken: cancellationToken)))
            : Task.CompletedTask;
    }

    /// <summary>
    /// Sets faulted.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SetFaultedAsync(BehaviorContext<FutureState, Fault<TRequest>> context, CancellationToken cancellationToken = default)
    {
        return _fault.SetFaultedAsync(context, cancellationToken: cancellationToken);
    }
}
