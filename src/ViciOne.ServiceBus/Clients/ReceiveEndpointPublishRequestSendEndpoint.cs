using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// Provides a receive endpoint publish request send endpoint implementation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class ReceiveEndpointPublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly HostReceiveEndpointHandle _handle;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handle">The handle value.</param>
    /// <param name="consumeContext">The consume context value.</param>
    public ReceiveEndpointPublishRequestSendEndpoint(HostReceiveEndpointHandle handle, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _handle = handle;
    }

    /// <summary>
    /// Gets send endpoint.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        var ready = await _handle.Ready.ConfigureAwait(false);

        return await ready.ReceiveEndpoint.GetPublishSendEndpointAsync<TRequest>().ConfigureAwait(false);
    }
}
