using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects one accepted request response to future result processing.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TFault">The terminal future fault contract.</typeparam>
/// <typeparam name="TRequest">The outbound request contract.</typeparam>
/// <typeparam name="TResponse">The accepted response contract.</typeparam>
internal sealed class FutureResponseConfigurator<TCommand, TResult, TFault, TRequest, TResponse> :
    IFutureResponseHandle<TCommand, TResult, TFault, TRequest, TResponse>,
    IFutureResponseConfigurator<TResult, TResponse>,
    ISpecification
    where TCommand : class
    where TResult : class
    where TFault : class
    where TRequest : class
    where TResponse : class
{
    readonly IFutureStateMachineConfigurator _configurator;
    readonly IFutureRequestHandle<TCommand, TResult, TFault, TRequest> _request;
    FutureResult<TCommand, TResult, TResponse>? _result;

    /// <summary>Creates response processing connected to its request configuration.</summary>
    /// <param name="configurator">The future state-machine configurator.</param>
    /// <param name="request">The request handle that owns this response.</param>
    public FutureResponseConfigurator(IFutureStateMachineConfigurator configurator, IFutureRequestHandle<TCommand, TResult, TFault, TRequest> request)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(request);
        _configurator = configurator;
        _request = request;

        Completed = configurator.CreateResponseEvent<TResponse>();
    }

    /// <summary>Gets the selector for the pending identifier completed by this response.</summary>
    public PendingFutureIdProvider<TResponse>? PendingResponseIdProvider { get; private set; }
    /// <summary>Gets the event raised when this response is received.</summary>
    public IEvent<TResponse> Completed { get; }

    /// <summary>Gets the event raised when the corresponding request faults.</summary>
    public IEvent<Fault<TRequest>> Faulted => _request.Faulted;

    /// <summary>Adds another accepted response contract to the same request.</summary>
    /// <typeparam name="T">The additional response contract.</typeparam>
    /// <param name="configure">The callback that configures response processing.</param>
    /// <returns>A handle for the configured response.</returns>
    public IFutureResponseHandle<TCommand, TResult, TFault, TRequest, T> OnResponseReceived<T>(
        Action<IFutureResponseConfigurator<TResult, T>> configure)
        where T : class
    {
        return _request.OnResponseReceived(configure);
    }

    /// <summary>Completes the pending request whose identifier is extracted from the response.</summary>
    /// <param name="provider">The selector for the completed operation identifier.</param>
    public void CompletePendingRequest(PendingFutureIdProvider<TResponse> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        PendingResponseIdProvider = provider;
    }

    /// <summary>Adds state-machine activities executed when the response is received.</summary>
    /// <param name="configure">The callback that adds activities to the response event.</param>
    public void WhenReceived(Func<IEventActivityBinder<FutureState, TResponse>, IEventActivityBinder<FutureState, TResponse>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configurator.DuringAnyWhen(Completed, configure);
    }

    /// <summary>Sets the synchronous factory that creates the successful future result.</summary>
    /// <param name="factoryMethod">The result factory invoked for this response.</param>
    public void SetResultFactory(EventMessageFactory<FutureState, TResponse, TResult> factoryMethod)
    {
        GetResultConfigurator().SetResultFactory(factoryMethod);
    }

    /// <summary>Sets the asynchronous factory that creates the successful future result.</summary>
    /// <param name="factoryMethod">The asynchronous result factory invoked for this response.</param>
    public void SetResultFactory(AsyncEventMessageFactory<FutureState, TResponse, TResult> factoryMethod)
    {
        GetResultConfigurator().SetResultFactory(factoryMethod);
    }

    /// <summary>Sets the initializer values used to create the successful future result.</summary>
    /// <param name="valueProvider">The provider of additional result property values.</param>
    public void SetResultInitializer(InitializerValueProvider<TResponse> valueProvider)
    {
        GetResultConfigurator().SetResultInitializer(valueProvider);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_result is null && PendingResponseIdProvider is null)
        {
            yield return this.Failure(
                "Response",
                "Result",
                "SetResultFactory, SetResultInitializer, SetResultFromInput, or CompletePendingRequest must be configured");
        }

        if (_result is not null)
        {
            foreach (ValidationResult result in _result.Validate())
                yield return result.WithParentKey("Response");
        }
    }

    /// <summary>Creates, sends, and stores the successful future result for this response.</summary>
    /// <param name="context">The response event context used to create the result.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetResultAsync(IBehaviorContext<FutureState, TResponse> context, CancellationToken cancellationToken = default)
    {
        return (_result ?? throw new InvalidOperationException("The response has no configured future result."))
            .SetResultAsync(context, cancellationToken: cancellationToken);
    }

    IFutureResultConfigurator<TResult, TResponse> GetResultConfigurator()
    {
        _result ??= new FutureResult<TCommand, TResult, TResponse>();

        return new FutureResultConfigurator<TCommand, TResult, TResponse>(_result);
    }
}
