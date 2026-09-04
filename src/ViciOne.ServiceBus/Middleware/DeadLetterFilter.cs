using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// If a message was neither delivered to a consumer nor caused a fault (which was notified already)
/// then this filter will send the message to the dead letter pipe.
/// </summary>
public class DeadLetterFilter :
    IFilter<ReceiveContext>
{
    readonly IPipe<ReceiveContext> _deadLetterPipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="deadLetterPipe">The dead letter pipe value.</param>
    public DeadLetterFilter(IPipe<ReceiveContext> deadLetterPipe)
    {
        _deadLetterPipe = deadLetterPipe;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("deadLetter");

        _deadLetterPipe.Probe(scope);
    }

    [DebuggerNonUserCode]
    async Task IFilter<ReceiveContext>.SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        await next.SendAsync(context).ConfigureAwait(false);

        if (context.IsDelivered || context.IsFaulted)
            return;

        await _deadLetterPipe.SendAsync(context).ConfigureAwait(false);

        context.LogSkipped();
    }
}
