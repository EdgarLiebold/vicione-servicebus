using System;
using System.Threading;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// The fault handler for the request client
/// </summary>
public class FaultHandlerConnectHandle :
    HandlerConnectHandle
{
    readonly ConnectHandle _handle;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handle">The handle value.</param>
    public FaultHandlerConnectHandle(ConnectHandle handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        _handle.Dispose();
    }

    /// <summary>
    /// Performs the disconnect operation.
    /// </summary>
    public void Disconnect()
    {
        _handle.Disconnect();
    }

    /// <summary>
    /// Performs the try set exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void TrySetException(Exception exception)
    {
    }

    /// <summary>
    /// Performs the try set canceled operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public void TrySetCanceled(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
    }
}
