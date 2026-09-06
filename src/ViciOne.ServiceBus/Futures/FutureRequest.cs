using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Carries the request for future.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TRequest">The request type.</typeparam>
public class FutureRequest<TInput, TRequest> :
    ISpecification
    where TRequest : class
    where TInput : class
{
    ContextMessageFactory<BehaviorContext<FutureState, TInput>, TRequest> _factory;

    /// <summary>Initializes a new instance.</summary>
    public FutureRequest()
    {
        _factory = new ContextMessageFactory<BehaviorContext<FutureState, TInput>, TRequest>(DefaultFactoryAsync);

        AddressProvider = PublishAddressProvider;
    }

    /// <summary>Gets or sets the address provider.</summary>
    public RequestAddressProvider<TInput> AddressProvider { get; set; }

    /// <summary>Gets or sets the pending request id provider.</summary>
    public PendingFutureIdProvider<TRequest> PendingRequestIdProvider { get; set; } = null!;
    /// <summary>Gets or sets the factory.</summary>
    public ContextMessageFactory<BehaviorContext<FutureState, TInput>, TRequest> Factory
    {
        set => _factory = value;
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

    /// <summary>Sends request.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendRequestAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        var destinationAddress = AddressProvider(context);

        var endpoint = destinationAddress != null
            ? await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await context.ReceiveContext.PublishEndpointProvider.GetPublishEndpointAsync<TRequest>(context, default);

        await _factory.UseAsync(context, async (ctx, s) =>
        {
            var pipe = new FutureRequestPipe<TRequest>(s.Pipe, context.ReceiveContext.InputAddress, context.Saga.CorrelationId);

            await endpoint.SendAsync(s.Message, pipe, ctx.CancellationToken).ConfigureAwait(false);

            if (PendingRequestIdProvider != null)
            {
                var pendingId = PendingRequestIdProvider(s.Message);
                context.Saga.Pending.Add(pendingId);
            }
        }, cancellationToken: cancellationToken);
    }

    static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>> DefaultFactoryAsync(BehaviorContext<FutureState, TInput> context)
    {
        return context.InitAsync<TRequest>(context.Message);
    }
}
