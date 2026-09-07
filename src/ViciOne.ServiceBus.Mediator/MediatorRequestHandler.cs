using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Mediator;

/// <summary>
/// Adapts a mediator request consumer to a message-and-cancellation-token handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public abstract class MediatorRequestHandler<TRequest> :
    IConsumer<TRequest>
    where TRequest : class
{
    /// <summary>Passes the consumed request to <see cref="HandleAsync" />.</summary>
    /// <param name="context">The request delivery context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<TRequest> context)
    {
        return HandleAsync(context.Message, context.CancellationToken);
    }

    /// <summary>Handles a mediator request that does not produce a response.</summary>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">The token that cancels request handling.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected abstract Task HandleAsync(TRequest request, CancellationToken cancellationToken);
}


/// <summary>
/// Adapts a mediator request consumer to a typed request-response handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class MediatorRequestHandler<TRequest, TResponse> :
    IConsumer<TRequest>
    where TRequest : class, IRequest<TResponse>
    where TResponse : class
{
    /// <summary>Handles the consumed request and sends the returned response.</summary>
    /// <param name="context">The request delivery context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<TRequest> context)
    {
        var response = await HandleAsync(context.Message, context.CancellationToken).ConfigureAwait(false);

        await context.RespondAsync(response).ConfigureAwait(false);
    }

    /// <summary>Handles a mediator request and creates its response.</summary>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">The token that cancels request handling.</param>
    /// <returns>A task that produces the response message.</returns>
    protected abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
