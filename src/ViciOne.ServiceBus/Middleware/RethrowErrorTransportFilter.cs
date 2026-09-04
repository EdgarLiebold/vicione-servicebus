using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Middleware;

public class RethrowErrorTransportFilter :
    IFilter<ExceptionReceiveContext>
{
    public async Task Send(ExceptionReceiveContext context, IPipe<ExceptionReceiveContext> next)
    {
        if (!context.IsFaulted)
            await context.NotifyFaulted(context.Exception).ConfigureAwait(false);

        context.Exception.Rethrow();
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("log-fault");
    }
}
