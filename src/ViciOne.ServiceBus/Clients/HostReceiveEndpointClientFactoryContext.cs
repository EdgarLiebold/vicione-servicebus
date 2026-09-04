using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// Provides a host receive endpoint client factory context implementation.
/// </summary>
public class HostReceiveEndpointClientFactoryContext :
    ReceiveEndpointClientFactoryContext,
    IAsyncDisposable
{
    readonly HostReceiveEndpointHandle _handle;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handle">The handle value.</param>
    /// <param name="defaultTimeout">The default timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public HostReceiveEndpointClientFactoryContext(
        HostReceiveEndpointHandle handle,
        RequestTimeout defaultTimeout = default,
        TimeProvider? timeProvider = null)
        : base(handle, defaultTimeout, timeProvider)
    {
        _handle = handle;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await _handle.StopAsync().ConfigureAwait(false);
    }
}
