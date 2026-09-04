using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.SqlTransport.Middleware;

/// <summary>
/// Adds the service bus message scheduler filter
/// </summary>
public class SqlMessageSchedulerFilter :
    IFilter<ConsumeContext>
{
    void IProbeSite.Probe(ProbeContext context)
    {
        context.CreateFilterScope("sqlScheduler");
    }

    [DebuggerNonUserCode]
    Task IFilter<ConsumeContext>.Send(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        context.GetOrAddPayload<MessageSchedulerContext>(() => new ConsumeMessageSchedulerContext(context, SchedulerFactory));

        return next.Send(context);
    }

    static IMessageScheduler SchedulerFactory(ConsumeContext context)
    {
        return new MessageScheduler(new SqlScheduleMessageProvider(context), context.GetPayload<IBusTopology>(), context.GetTimeProvider());
    }
}
