using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures future response.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TFault">The fault type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class FutureResponseConfigurator<TCommand, TResult, TFault, TRequest, TResponse> :
    FutureResponseHandle<TCommand, TResult, TFault, TRequest, TResponse>,
    IFutureResponseConfigurator<TResult, TResponse>,
    ISpecification
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
    where TResponse : class
{
    readonly IFutureStateMachineConfigurator _configurator;
    readonly FutureRequestHandle<TCommand, TResult, TFault, TRequest> _request;
    FutureResult<TCommand, TResult, TResponse> _result = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="request">The request.</param>
    public FutureResponseConfigurator(IFutureStateMachineConfigurator configurator, FutureRequestHandle<TCommand, TResult, TFault, TRequest> request)
    {
        _configurator = configurator;
        _request = request;

        Completed = configurator.CreateResponseEvent<TResponse>();
    }

    /// <summary>Gets or sets the pending response id provider.</summary>
    public PendingFutureIdProvider<TResponse> PendingResponseIdProvider { get; private set; } = null!;
    /// <summary>Gets the completed.</summary>
    public Event<TResponse> Completed { get; }

    /// <summary>Gets the faulted.</summary>
    public Event<Fault<TRequest>> Faulted => _request.Faulted;

    /// <summary>Handles the notification for response received.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The future response handle produced by the operation.</returns>
    public FutureResponseHandle<TCommand, TResult, TFault, TRequest, T> OnResponseReceived<T>(
        Action<IFutureResponseConfigurator<TResult, T>>? configure = default)
        where T : class
    {
        return _request.OnResponseReceived(configure);
    }

    /// <summary>Completes pending request.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public void CompletePendingRequest(PendingFutureIdProvider<TResponse> provider)
    {
        PendingResponseIdProvider = provider;
    }

    /// <summary>Adds behavior that runs when the message is received.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void WhenReceived(Func<EventActivityBinder<FutureState, TResponse>, EventActivityBinder<FutureState, TResponse>> configure)
    {
        _configurator.DuringAnyWhen(Completed, configure);
    }

    /// <summary>Sets completed using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetCompletedUsingFactory(EventMessageFactory<FutureState, TResponse, TResult> factoryMethod)
    {
        GetResultConfigurator().SetCompletedUsingFactory(factoryMethod);
    }

    /// <summary>Sets completed using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetCompletedUsingFactory(AsyncEventMessageFactory<FutureState, TResponse, TResult> factoryMethod)
    {
        GetResultConfigurator().SetCompletedUsingFactory(factoryMethod);
    }

    /// <summary>Sets completed using initializer.</summary>
    /// <param name="valueProvider">The value provider.</param>
    public void SetCompletedUsingInitializer(InitializerValueProvider<TResponse> valueProvider)
    {
        GetResultConfigurator().SetCompletedUsingInitializer(valueProvider);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Sets result.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetResultAsync(BehaviorContext<FutureState, TResponse> context, CancellationToken cancellationToken = default)
    {
        return _result.SetResultAsync(context, cancellationToken: cancellationToken);
    }

    IFutureResultConfigurator<TResult, TResponse> GetResultConfigurator()
    {
        _result ??= new FutureResult<TCommand, TResult, TResponse>();

        return new FutureResultConfigurator<TCommand, TResult, TResponse>(_result);
    }
}
