using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients.Endpoints;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Routes request-client sends through the mediator endpoint.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal sealed class MediatorRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly ISendEndpoint _endpoint;

    /// <summary>Initializes request routing over a mediator send endpoint.</summary>
    /// <param name="endpoint">The mediator send endpoint.</param>
    /// <param name="consumeContext">The optional consume context that initiated the request.</param>
    public MediatorRequestSendEndpoint(ISendEndpoint endpoint, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    /// <summary>Returns the mediator send endpoint.</summary>
    /// <param name="cancellationToken">Cancels endpoint resolution before the endpoint is returned.</param>
    /// <returns>A completed task containing the mediator endpoint.</returns>
    protected override Task<ISendEndpoint> GetSendEndpointAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_endpoint);
    }
}
