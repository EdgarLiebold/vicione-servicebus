using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Simply ignores/discards the not-consumed message
/// </summary>
public class DiscardDeadLetterFilter :
    IFilter<ReceiveContext>
{
    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("discard-dead-letter");
    }

    Task IFilter<ReceiveContext>.SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        return next.SendAsync(context);
    }
}
