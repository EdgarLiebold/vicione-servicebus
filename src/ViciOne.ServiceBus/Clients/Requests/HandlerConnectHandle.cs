using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients.Requests;

/// <summary>Owns one typed response-handler registration and its response task.</summary>
/// <typeparam name="T">The response message contract.</typeparam>
internal interface HandlerConnectHandle<T> :
    HandlerConnectHandle
    where T : class
{
    /// <summary>Gets the task that completes with the matching response.</summary>
    Task<Response<T>> Task { get; }
}

/// <summary>Completes and disconnects one response-handler registration.</summary>
internal interface HandlerConnectHandle :
    ConnectHandle
{
    /// <summary>Attempts to complete the response handler with a request failure.</summary>
    /// <param name="exception">The request failure.</param>
    void TrySetException(Exception exception);

    /// <summary>Attempts to cancel the response handler with the originating token.</summary>
    /// <param name="cancellationToken">The token that canceled response waiting.</param>
    void TrySetCanceled(CancellationToken cancellationToken);
}
