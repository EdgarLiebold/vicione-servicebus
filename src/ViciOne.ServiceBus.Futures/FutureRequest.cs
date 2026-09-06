using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Creates and dispatches one request from a future input.</summary>
/// <typeparam name="TInput">The future event contract used to create the request.</typeparam>
/// <typeparam name="TRequest">The request contract to dispatch.</typeparam>
internal sealed class FutureRequest<TInput, TRequest> :
    ISpecification
    where TRequest : class
    where TInput : class
{
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TRequest> _factory;

    /// <summary>Creates a request dispatcher that publishes a request initialized from the input by default.</summary>
    public FutureRequest()
    {
        _factory = new ContextMessageFactory<BehaviorContext<FutureState, TInput>, TRequest>(DefaultFactoryAsync);

        AddressProvider = PublishAddressProvider;
    }

    /// <summary>Gets or sets the selector for a send destination; returning <see langword="null" /> publishes the request.</summary>
    public RequestAddressProvider<TInput> AddressProvider { get; set; }

    /// <summary>Gets or sets the selector for an identifier that remains pending until a matching response arrives.</summary>
    public PendingFutureIdProvider<TRequest>? PendingRequestIdProvider { get; set; }
    /// <summary>Sets the factory that creates the outbound request from the future event.</summary>
    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TRequest> Factory
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _factory = value;
        }
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_factory == null)
            yield return this.Failure("Response", "Factory", "Init or Create must be configured");
        if (AddressProvider == null)
            yield return this.Failure("RequestAddressProvider", "must not be null");
    }

    static Uri? PublishAddressProvider<T>(BehaviorContext<FutureState, T> context)
        where T : class
    {
        return default;
    }

    /// <summary>Creates and dispatches the request, then records its pending identifier when tracking is configured.</summary>
    /// <param name="context">The future event context used to create and dispatch the request.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendRequestAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var destinationAddress = AddressProvider(context);

        var endpoint = destinationAddress != null
            ? await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await context.ReceiveContext.PublishEndpointProvider
                .GetPublishEndpointAsync<TRequest>(context, requestId: null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        await _factory.UseAsync(context, async (ctx, s) =>
        {
            var pipe = new FutureRequestPipe<TRequest>(s.Pipe, context.ReceiveContext.InputAddress, context.Saga.CorrelationId);

            await endpoint.SendAsync(s.Message, pipe, cancellationToken).ConfigureAwait(false);

            if (PendingRequestIdProvider != null)
            {
                var pendingId = PendingRequestIdProvider(s.Message);
                context.Saga.Pending.Add(pendingId);
            }
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>> DefaultFactoryAsync(BehaviorContext<FutureState, TInput> context)
    {
        return context.InitAsync<TRequest>(context.Message);
    }
}
