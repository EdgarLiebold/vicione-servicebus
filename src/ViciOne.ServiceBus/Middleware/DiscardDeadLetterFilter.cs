// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    using System.Threading.Tasks;


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

        Task IFilter<ReceiveContext>.Send(ReceiveContext context, IPipe<ReceiveContext> next)
        {
            return next.Send(context);
        }
    }
}
