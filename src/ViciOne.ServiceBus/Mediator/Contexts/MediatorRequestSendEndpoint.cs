using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Provides an endpoint for mediator request send.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public class MediatorRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly ISendEndpoint _endpoint;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="consumeContext">The consume context.</param>
    public MediatorRequestSendEndpoint(ISendEndpoint endpoint, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _endpoint = endpoint;
    }

    /// <summary>Gets send endpoint.</summary>
    /// <returns>A task that produces the requested value.</returns>
    protected override Task<ISendEndpoint> GetSendEndpointAsync()
    {
        return Task.FromResult(_endpoint);
    }
}
