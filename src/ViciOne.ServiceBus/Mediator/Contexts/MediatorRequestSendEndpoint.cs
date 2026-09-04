using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>
/// Provides a mediator request send endpoint implementation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class MediatorRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly ISendEndpoint _endpoint;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="consumeContext">The consume context value.</param>
    public MediatorRequestSendEndpoint(ISendEndpoint endpoint, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _endpoint = endpoint;
    }

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override Task<ISendEndpoint> GetSendEndpointAsync()
    {
        return Task.FromResult(_endpoint);
    }
}
