using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides an endpoint for scoped request send.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public class ScopedRequestSendEndpoint<TRequest> :
    IRequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly IRequestSendEndpoint<TRequest> _endpoint;
    readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public ScopedRequestSendEndpoint(IRequestSendEndpoint<TRequest> endpoint, IServiceProvider serviceProvider)
    {
        _endpoint = endpoint;
        _serviceProvider = serviceProvider;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the send outcome.</returns>
    public Task<TRequest> SendAsync(Guid requestId, object values, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(requestId, values, new ScopedSendPipeAdapter<TRequest>(_serviceProvider, pipe), cancellationToken);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(Guid requestId, TRequest message, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken)
    {
        return _endpoint.SendAsync(requestId, message, new ScopedSendPipeAdapter<TRequest>(_serviceProvider, pipe), cancellationToken);
    }
}
