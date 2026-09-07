using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Connects request clients to a temporary receive endpoint and owns its lifetime.</summary>
internal sealed class HostReceiveEndpointClientFactoryContext :
    ReceiveEndpointClientFactoryContext,
    IAsyncDisposable
{
    readonly HostReceiveEndpointHandle _handle;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handle">The handle.</param>
    /// <param name="defaultTimeout">The default timeout.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public HostReceiveEndpointClientFactoryContext(
        HostReceiveEndpointHandle handle,
        RequestTimeout defaultTimeout = default,
        TimeProvider? timeProvider = null)
        : base(handle, defaultTimeout, timeProvider)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _handle.StopAsync().ConfigureAwait(false);
    }
}
