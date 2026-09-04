using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public class FaultDeadLetterFilter :
    IFilter<ReceiveContext>
{
    public Task SendAsync(ReceiveContext context, IPipe<ReceiveContext> next)
    {
        throw new MessageNotConsumedException(context.InputAddress, "The message was not consumed");
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("fault-not-consumed");
    }
}
