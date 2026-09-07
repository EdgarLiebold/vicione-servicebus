using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Provides an endpoint for receive endpoint publish request send.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal sealed class ReceiveEndpointPublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly HostReceiveEndpointHandle _handle;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handle">The handle.</param>
    /// <param name="consumeContext">The consume context.</param>
    public ReceiveEndpointPublishRequestSendEndpoint(HostReceiveEndpointHandle handle, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>Gets send endpoint.</summary>
    /// <returns>A task that produces the requested value.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        var ready = await _handle.Ready.ConfigureAwait(false);

        return await ready.ReceiveEndpoint.GetPublishSendEndpointAsync<TRequest>().ConfigureAwait(false);
    }
}
