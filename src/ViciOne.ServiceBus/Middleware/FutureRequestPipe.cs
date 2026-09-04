using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a future request pipe implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class FutureRequestPipe<T> :
    IPipe<SendContext<T>>
    where T : class
{
    readonly IPipe<SendContext<T>> _pipe;
    readonly Guid _requestId;
    readonly Uri _responseAddress;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="responseAddress">The response address value.</param>
    /// <param name="requestId">The request id value.</param>
    public FutureRequestPipe(IPipe<SendContext<T>> pipe, Uri responseAddress, Guid requestId)
    {
        _pipe = pipe;
        _responseAddress = responseAddress;
        _requestId = requestId;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendContext<T> context)
    {
        context.ResponseAddress = _responseAddress;
        context.RequestId = _requestId;

        return _pipe.IsNotEmpty() ? _pipe.SendAsync(context) : Task.CompletedTask;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope(nameof(FutureRequestPipe<T>));
    }
}
