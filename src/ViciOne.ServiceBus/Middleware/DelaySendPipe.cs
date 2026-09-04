using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a delay send pipe implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class DelaySendPipe<T> :
    IPipe<SendContext<T>>
    where T : class
{
    readonly TimeSpan _delay;
    readonly IPipe<SendContext<T>> _pipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="delay">The delay value.</param>
    public DelaySendPipe(IPipe<SendContext<T>> pipe, TimeSpan delay)
    {
        _pipe = pipe;
        _delay = delay;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _pipe?.Probe(context);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(SendContext<T> context)
    {
        if (_delay > TimeSpan.Zero)
            context.Delay = _delay;

        return _pipe.IsNotEmpty() ? _pipe.SendAsync(context) : Task.CompletedTask;
    }
}
