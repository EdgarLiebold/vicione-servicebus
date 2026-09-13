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
    ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TRequest> _factory;

    /// <summary>Creates a request dispatcher that publishes a request initialized from the input by default.</summary>
    public FutureRequest()
    {
        _factory = new ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TRequest>(DefaultFactoryAsync);

        AddressProvider = PublishAddressProvider;
    }

    /// <summary>Gets or sets the selector for a send destination; returning <see langword="null" /> publishes the request.</summary>
    public RequestAddressProvider<TInput> AddressProvider { get; set; }

    /// <summary>Gets or sets the selector for an identifier that remains pending until a matching response arrives.</summary>
    public PendingFutureIdProvider<TRequest>? PendingRequestIdProvider { get; set; }
    /// <summary>Sets the factory that creates the outbound request from the future event.</summary>
    public ContextMessageFactory<IBehaviorContext<FutureState, TInput>, TRequest> Factory
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
        if (AddressProvider == null)
            yield return this.Failure("RequestAddressProvider", "must not be null");
    }

    static Uri? PublishAddressProvider<T>(IBehaviorContext<FutureState, T> context)
        where T : class
    {
        return default;
    }

    /// <summary>Creates the request, records its pending identifier, and dispatches it.</summary>
    /// <param name="context">The future event context used to create and dispatch the request.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendRequestAsync(IBehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        RequestAddressProvider<TInput> addressProvider = AddressProvider
            ?? throw new InvalidOperationException("The future request address provider has not been configured.");
        var destinationAddress = addressProvider(context);
        if (destinationAddress is { IsAbsoluteUri: false })
            throw new ArgumentException("The future request address provider must return an absolute URI or null.", nameof(AddressProvider));

        var endpoint = destinationAddress != null
            ? await context.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false)
            : await context.ReceiveContext.PublishEndpointProvider
                .GetPublishEndpointAsync<TRequest>(context, requestId: null, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

        await _factory.UseAsync(context, async (ctx, s) =>
        {
            var pipe = new FutureRequestPipe<TRequest>(s.Pipe, context.ReceiveContext.InputAddress, context.Saga.CorrelationId);
            Guid? pendingId = PendingRequestIdProvider?.Invoke(s.Message);
            if (pendingId == Guid.Empty)
                throw new InvalidOperationException("A tracked future request must provide a nonempty pending operation identifier.");
            if (pendingId.HasValue && !context.Saga.Pending.Add(pendingId.Value))
            {
                throw new InvalidOperationException(
                    $"The future already contains pending operation '{pendingId.Value}'.");
            }

            try
            {
                await endpoint.SendAsync(s.Message, pipe, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (pendingId.HasValue)
                    context.Saga.Pending.Remove(pendingId.Value);

                throw;
            }
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    static Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TRequest>> DefaultFactoryAsync(IBehaviorContext<FutureState, TInput> context)
    {
        return context.InitAsync<TRequest>(context.Message);
    }
}
