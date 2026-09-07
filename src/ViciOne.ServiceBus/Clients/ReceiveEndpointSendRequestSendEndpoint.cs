using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Resolves an explicit request destination after a connected response endpoint becomes ready.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
internal sealed class ReceiveEndpointSendRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly Uri _destinationAddress;
    readonly IHostReceiveEndpointHandle _handle;

    /// <summary>Creates a request endpoint backed by a connected response endpoint.</summary>
    /// <param name="handle">The response endpoint handle whose readiness gates destination resolution.</param>
    /// <param name="destinationAddress">The request service address.</param>
    /// <param name="consumeContext">The consume context whose request metadata is propagated, or <see langword="null" />.</param>
    public ReceiveEndpointSendRequestSendEndpoint(IHostReceiveEndpointHandle handle, Uri destinationAddress, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _destinationAddress = destinationAddress ?? throw new ArgumentNullException(nameof(destinationAddress));
    }

    /// <summary>Waits for the response endpoint and resolves the explicit request destination.</summary>
    /// <returns>A task that produces the request send endpoint.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        var ready = await _handle.Ready.ConfigureAwait(false);

        return await ready.ReceiveEndpoint.GetSendEndpointAsync(_destinationAddress).ConfigureAwait(false);
    }
}
