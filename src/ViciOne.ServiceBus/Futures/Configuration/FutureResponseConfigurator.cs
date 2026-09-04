using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a future response configurator implementation.
/// </summary>
/// <typeparam name="TCommand">The t command type.</typeparam>
/// <typeparam name="TResult">The t result type.</typeparam>
/// <typeparam name="TFault">The t fault type.</typeparam>
/// <typeparam name="TRequest">The t request type.</typeparam>
/// <typeparam name="TResponse">The t response type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="request">The request value.</param>
    public FutureResponseConfigurator(IFutureStateMachineConfigurator configurator, FutureRequestHandle<TCommand, TResult, TFault, TRequest> request)
    {
        _configurator = configurator;
        _request = request;

        Completed = configurator.CreateResponseEvent<TResponse>();
    }

    /// <summary>
    /// Gets or sets the pending response id provider value.
    /// </summary>
    public PendingFutureIdProvider<TResponse> PendingResponseIdProvider { get; private set; } = null!;
    /// <summary>
    /// Gets the completed value.
    /// </summary>
    public Event<TResponse> Completed { get; }

    /// <summary>
    /// Gets the faulted value.
    /// </summary>
    public Event<Fault<TRequest>> Faulted => _request.Faulted;

    /// <summary>
    /// Performs the on response received operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public FutureResponseHandle<TCommand, TResult, TFault, TRequest, T> OnResponseReceived<T>(
        Action<IFutureResponseConfigurator<TResult, T>>? configure = default)
        where T : class
    {
        return _request.OnResponseReceived(configure);
    }

    /// <summary>
    /// Performs the complete pending request operation.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public void CompletePendingRequest(PendingFutureIdProvider<TResponse> provider)
    {
        PendingResponseIdProvider = provider;
    }

    /// <summary>
    /// Performs the when received operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void WhenReceived(Func<EventActivityBinder<FutureState, TResponse>, EventActivityBinder<FutureState, TResponse>> configure)
    {
        _configurator.DuringAnyWhen(Completed, configure);
    }

    /// <summary>
    /// Sets completed using factory.
    /// </summary>
    /// <param name="factoryMethod">The factory method value.</param>
    public void SetCompletedUsingFactory(EventMessageFactory<FutureState, TResponse, TResult> factoryMethod)
    {
        GetResultConfigurator().SetCompletedUsingFactory(factoryMethod);
    }

    /// <summary>
    /// Sets completed using factory.
    /// </summary>
    /// <param name="factoryMethod">The factory method value.</param>
    public void SetCompletedUsingFactory(AsyncEventMessageFactory<FutureState, TResponse, TResult> factoryMethod)
    {
        GetResultConfigurator().SetCompletedUsingFactory(factoryMethod);
    }

    /// <summary>
    /// Sets completed using initializer.
    /// </summary>
    /// <param name="valueProvider">The value provider value.</param>
    public void SetCompletedUsingInitializer(InitializerValueProvider<TResponse> valueProvider)
    {
        GetResultConfigurator().SetCompletedUsingInitializer(valueProvider);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Sets result.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
