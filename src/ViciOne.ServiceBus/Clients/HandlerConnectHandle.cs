using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Controls the lifetime of handler connect.</summary>
/// <typeparam name="T">The value type.</typeparam>
internal interface HandlerConnectHandle<T> :
    HandlerConnectHandle
    where T : class
{
    /// <summary>Gets the task.</summary>
    Task<Response<T>> Task { get; }
}


/// <summary>Controls the lifetime of handler connect.</summary>
internal interface HandlerConnectHandle :
    ConnectHandle
{
    /// <summary>Attempts to set exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    void TrySetException(Exception exception);

    /// <summary>Attempts to set canceled.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    void TrySetCanceled(CancellationToken cancellationToken);
}
