using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Mediator;

/// <summary>
/// A Mediator request handler base class that provides a simplified overridable method with
/// a Task (void) return type
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public abstract class MediatorRequestHandler<TRequest> :
    IConsumer<TRequest>
    where TRequest : class
{
    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ConsumeAsync(ConsumeContext<TRequest> context)
    {
        return HandleAsync(context.Message, context.CancellationToken);
    }

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected abstract Task HandleAsync(TRequest request, CancellationToken cancellationToken);
}


/// <summary>
/// A Mediator request handler base class that provides a simplified overridable method with
/// a Task&lt;<typeparamref name="TResponse"/>&gt; return type
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public abstract class MediatorRequestHandler<TRequest, TResponse> :
    IConsumer<TRequest>
    where TRequest : class, Request<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<TRequest> context)
    {
        var response = await HandleAsync(context.Message, context.CancellationToken).ConfigureAwait(false);

        await context.RespondAsync(response).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
