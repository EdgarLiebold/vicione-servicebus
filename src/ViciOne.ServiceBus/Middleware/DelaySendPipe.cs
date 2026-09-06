using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Executes the pipeline for delay send.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class DelaySendPipe<T> :
    IPipe<SendContext<T>>
    where T : class
{
    readonly TimeSpan _delay;
    readonly IPipe<SendContext<T>> _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="delay">The delay before the operation is attempted.</param>
    public DelaySendPipe(IPipe<SendContext<T>> pipe, TimeSpan delay)
    {
        _pipe = pipe;
        _delay = delay;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _pipe?.Probe(context);
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SendAsync(SendContext<T> context)
    {
        if (_delay > TimeSpan.Zero)
            context.Delay = _delay;

        return _pipe.IsNotEmpty() ? _pipe.SendAsync(context) : Task.CompletedTask;
    }
}
