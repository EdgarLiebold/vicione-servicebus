using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Provides an endpoint for receive endpoint send request send.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal sealed class ReceiveEndpointSendRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly Uri _destinationAddress;
    readonly HostReceiveEndpointHandle _handle;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handle">The handle.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="consumeContext">The consume context.</param>
    public ReceiveEndpointSendRequestSendEndpoint(HostReceiveEndpointHandle handle, Uri destinationAddress, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _destinationAddress = destinationAddress ?? throw new ArgumentNullException(nameof(destinationAddress));
    }

    /// <summary>Gets send endpoint.</summary>
    /// <returns>A task that produces the requested value.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        var ready = await _handle.Ready.ConfigureAwait(false);

        return await ready.ReceiveEndpoint.GetSendEndpointAsync(_destinationAddress).ConfigureAwait(false);
    }
}
