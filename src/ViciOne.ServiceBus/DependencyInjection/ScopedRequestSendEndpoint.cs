using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a scoped request send endpoint implementation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class ScopedRequestSendEndpoint<TRequest> :
    IRequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly IRequestSendEndpoint<TRequest> _endpoint;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="serviceProvider">The service provider value.</param>
    public ScopedRequestSendEndpoint(IRequestSendEndpoint<TRequest> endpoint, IServiceProvider serviceProvider)
    {
        _endpoint = endpoint;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="requestId">The request id value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TRequest> SendAsync(Guid requestId, object values, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(requestId, values, new ScopedSendPipeAdapter<TRequest>(_serviceProvider, pipe), cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="requestId">The request id value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(Guid requestId, TRequest message, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(requestId, message, new ScopedSendPipeAdapter<TRequest>(_serviceProvider, pipe), cancellationToken);
    }
}
