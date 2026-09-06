using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a request issued by a future and its response and fault paths.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TFault">The terminal future fault contract.</typeparam>
/// <typeparam name="TInput">The future event contract used to create the request.</typeparam>
/// <typeparam name="TRequest">The outbound request contract.</typeparam>
internal sealed class FutureRequestConfigurator<TCommand, TResult, TFault, TInput, TRequest> :
    IFutureRequestHandle<TCommand, TResult, TFault, TRequest>,
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

    /// <summary>Creates request, response, and fault configuration for a future event.</summary>
    /// <param name="configurator">The future state-machine configurator.</param>
    /// <param name="faulted">The event raised when the outbound request faults.</param>
    public FutureRequestConfigurator(IFutureStateMachineConfigurator configurator, Event<Fault<TRequest>> faulted)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(faulted);
        _configurator = configurator;

        Faulted = faulted;

        _request = new FutureRequest<TInput, TRequest>();
        _fault = new FutureFault<TCommand, TFault, Fault<TRequest>>();
    }

    /// <summary>Gets the selector for the identifier recorded while the request is pending.</summary>
    public PendingFutureIdProvider<TRequest>? PendingRequestIdProvider
    {
        get => _request.PendingRequestIdProvider;
        private set => _request.PendingRequestIdProvider = value;
    }

    /// <summary>Gets the event raised when the outbound request faults.</summary>
    public Event<Fault<TRequest>> Faulted { get; }

    /// <summary>Adds an accepted response contract and configures how it advances the future.</summary>
    /// <typeparam name="TResponse">The accepted response contract.</typeparam>
    /// <param name="configure">The callback that configures response processing.</param>
    /// <returns>A handle for the configured response.</returns>
    public IFutureResponseHandle<TCommand, TResult, TFault, TRequest, TResponse>
        OnResponseReceived<TResponse>(Action<IFutureResponseConfigurator<TResult, TResponse>> configure)
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(configure);
        var response = new FutureResponseConfigurator<TCommand, TResult, TFault, TRequest, TResponse>(_configurator, this);

        configure(response);

        response.Validate().ThrowIfContainsFailure(
            $"The future request response was not configured correctly: {TypeCache.GetShortName(GetType())}");

        if (response.PendingResponseIdProvider != null)
            _configurator.CompletePendingRequest(response.Completed, response.PendingResponseIdProvider);
        else
            _configurator.SetResult(response.Completed, context => response.SetResultAsync(context, context.CancellationToken));

        return response;
    }

    /// <summary>Sets the fixed destination address for the outbound request.</summary>
    public Uri RequestAddress
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _request.AddressProvider = _ => value;
        }
    }

    /// <summary>Sets the context-dependent selector for the outbound request destination.</summary>
    /// <param name="provider">The selector that returns a destination, or <see langword="null" /> to publish.</param>
    public void SetRequestAddressProvider(RequestAddressProvider<TInput> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _request.AddressProvider = provider;
    }

    /// <summary>Sets the synchronous factory that creates the outbound request.</summary>
    /// <param name="factoryMethod">The factory that creates the request from the future event.</param>
    public void SetRequestFactory(EventMessageFactory<FutureState, TInput, TRequest> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);
        _request.Factory = MessageFactory<TRequest>.Create(factoryMethod);
    }

    /// <summary>Sets the asynchronous factory that creates the outbound request.</summary>
    /// <param name="factoryMethod">The asynchronous factory that creates the request from the future event.</param>
    public void SetRequestFactory(AsyncEventMessageFactory<FutureState, TInput, TRequest> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);
        _request.Factory = MessageFactory<TRequest>.Create(factoryMethod);
    }

    /// <summary>Sets the initializer values used to create the outbound request.</summary>
    /// <param name="valueProvider">The provider of additional request property values.</param>
    public void SetRequestInitializer(InitializerValueProvider<TInput> valueProvider)
    {
        ArgumentNullException.ThrowIfNull(valueProvider);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>> FactoryAsync(BehaviorContext<FutureState, TInput> context)
        {
            return MessageInitializerCache<TRequest>.InitializeMessageAsync(context, valueProvider(context), new object[] { context.Message });
        }

        _request.Factory = MessageFactory<TRequest>.Create((Func<BehaviorContext<FutureState, TInput>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>>>)FactoryAsync);
    }

    /// <summary>Records each outbound request as pending until a response supplies the same identifier.</summary>
    /// <param name="provider">The selector that extracts the pending-operation identifier from the request.</param>
    public void TrackPendingRequest(PendingFutureIdProvider<TRequest> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        PendingRequestIdProvider = provider;
    }

    /// <summary>Configures how a request fault creates the terminal future fault.</summary>
    /// <param name="configure">The callback that configures the future fault message.</param>
    public void OnRequestFaulted(Action<IFutureFaultConfigurator<TFault, Fault<TRequest>>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var configurator = new FutureFaultConfigurator<TCommand, TFault, Fault<TRequest>>(_fault);

        configure(configurator);
    }

    /// <summary>Adds state-machine activities executed when the request faults.</summary>
    /// <param name="configure">The callback that adds activities to the request-fault event.</param>
    public void WhenFaulted(Func<EventActivityBinder<FutureState, Fault<TRequest>>, EventActivityBinder<FutureState, Fault<TRequest>>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configurator.DuringAnyWhen(Faulted, configure);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _request.Validate();
    }

    /// <summary>Creates and dispatches a request directly from the current future event.</summary>
    /// <param name="context">The future event context used to create the request.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _request.SendRequestAsync(context, cancellationToken: cancellationToken);
    }

    /// <summary>Creates and dispatches a request from a selected command value.</summary>
    /// <param name="context">The initiating command context that owns the future.</param>
    /// <param name="data">The selected value exposed as the request's input event.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(BehaviorContext<FutureState, TCommand> context, TInput data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(data);
        return _request.SendRequestAsync(
            context.CreateProxy(MessageEvent<TInput>.Instance, data),
            cancellationToken: cancellationToken);
    }

    /// <summary>Creates and dispatches one request for every selected command value.</summary>
    /// <param name="context">The initiating command context that owns the future.</param>
    /// <param name="inputs">The selected values exposed as individual request input events.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendRangeAsync(BehaviorContext<FutureState, TCommand> context, IEnumerable<TInput> inputs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(inputs);
        cancellationToken.ThrowIfCancellationRequested();
        TInput[] inputSnapshot = inputs.ToArray();
        foreach (TInput input in inputSnapshot)
            ArgumentNullException.ThrowIfNull(input);

        return Task.WhenAll(inputSnapshot.Select(input => SendAsync(context, input, cancellationToken: cancellationToken)));
    }

    /// <summary>Creates and stores the terminal future fault for a failed request.</summary>
    /// <param name="context">The request-fault event context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetFaultedAsync(BehaviorContext<FutureState, Fault<TRequest>> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _fault.SetFaultedAsync(context, cancellationToken: cancellationToken);
    }
}
