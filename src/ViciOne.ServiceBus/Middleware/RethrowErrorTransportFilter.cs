// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    using System.Threading.Tasks;
    using Internals;


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
}
