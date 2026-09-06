using System;
using System.Threading;

namespace ViciOne.ServiceBus.Clients;

/// <summary>The fault handler for the request client.</summary>
public class FaultHandlerConnectHandle :
    HandlerConnectHandle
{
    readonly ConnectHandle _handle;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handle">The handle.</param>
    public FaultHandlerConnectHandle(ConnectHandle handle)
    {
        _handle = handle;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        _handle.Dispose();
    }

    /// <summary>Disconnects the current observer or endpoint.</summary>
    public void Disconnect()
    {
        _handle.Disconnect();
    }

    /// <summary>Attempts to set exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void TrySetException(Exception exception)
    {
    }

    /// <summary>Attempts to set canceled.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void TrySetCanceled(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
    }
}
