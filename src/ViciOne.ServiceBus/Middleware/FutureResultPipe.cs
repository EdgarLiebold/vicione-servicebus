using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for future result.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class FutureResultPipe<T> :
    IPipe<SendContext<T>>
    where T : class
{
    readonly IPipe<SendContext<T>> _pipe;
    readonly Guid _requestId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="requestId">The request id.</param>
    public FutureResultPipe(IPipe<SendContext<T>> pipe, Guid requestId)
    {
        _pipe = pipe;
        _requestId = requestId;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext<T> context)
    {
        context.RequestId = _requestId;

        return _pipe.IsNotEmpty() ? _pipe.SendAsync(context) : Task.CompletedTask;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope(nameof(FutureResultPipe<T>));
    }
}
