using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients.Contexts;

/// <summary>Connects request clients to a temporary receive endpoint and owns its lifetime.</summary>
internal sealed class HostReceiveEndpointClientFactoryContext :
    ReceiveEndpointClientFactoryContext,
    IAsyncDisposable
{
    readonly IHostReceiveEndpointHandle _handle;

    /// <summary>Creates a client-factory context that owns a connected response endpoint.</summary>
    /// <param name="handle">The connected endpoint that receives responses and is stopped on disposal.</param>
    /// <param name="defaultTimeout">The default request timeout.</param>
    /// <param name="timeProvider">The time source used to measure request deadlines.</param>
    public HostReceiveEndpointClientFactoryContext(
        IHostReceiveEndpointHandle handle,
        RequestTimeout defaultTimeout = default,
        TimeProvider? timeProvider = null)
        : base(handle, defaultTimeout, timeProvider)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>Stops and removes the response endpoint owned by this context.</summary>
    /// <returns>A task that completes after the endpoint has stopped.</returns>
    public async ValueTask DisposeAsync()
    {
        await _handle.StopAsync().ConfigureAwait(false);
    }
}
