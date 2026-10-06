using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for future request.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class FutureRequestPipe<T> :
    IPipe<SendContext<T>>
    where T : class
{
    readonly IPipe<SendContext<T>> _pipe;
    readonly Guid _requestId;
    readonly Uri _responseAddress;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="responseAddress">The response address.</param>
    /// <param name="requestId">The request id.</param>
    public FutureRequestPipe(IPipe<SendContext<T>> pipe, Uri responseAddress, Guid requestId)
    {
        _pipe = pipe;
        _responseAddress = responseAddress;
        _requestId = requestId;
    }

    /// <summary>Assigns request and response metadata before invoking an available send pipe.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext<T> context)
    {
        context.ResponseAddress = _responseAddress;
        context.RequestId = _requestId;

        return _pipe.IsNotEmpty() ? _pipe.SendAsync(context) : Task.CompletedTask;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope(nameof(FutureRequestPipe<T>));
    }
}
