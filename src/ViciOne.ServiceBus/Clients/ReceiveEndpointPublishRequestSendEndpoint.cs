using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Resolves the publish destination for requests after a connected response endpoint becomes ready.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
internal sealed class ReceiveEndpointPublishRequestSendEndpoint<TRequest> :
    RequestSendEndpoint<TRequest>
    where TRequest : class
{
    readonly IHostReceiveEndpointHandle _handle;

    /// <summary>Creates a request endpoint backed by a connected response endpoint.</summary>
    /// <param name="handle">The response endpoint handle whose readiness gates destination resolution.</param>
    /// <param name="consumeContext">The consume context whose request metadata is propagated, or <see langword="null" />.</param>
    public ReceiveEndpointPublishRequestSendEndpoint(IHostReceiveEndpointHandle handle, ConsumeContext? consumeContext)
        : base(consumeContext)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>Waits for the response endpoint and resolves the publish destination for the request contract.</summary>
    /// <returns>A task that produces the publish send endpoint.</returns>
    protected override async Task<ISendEndpoint> GetSendEndpointAsync()
    {
        var ready = await _handle.Ready.ConfigureAwait(false);

        return await ready.ReceiveEndpoint.GetPublishSendEndpointAsync<TRequest>().ConfigureAwait(false);
    }
}
