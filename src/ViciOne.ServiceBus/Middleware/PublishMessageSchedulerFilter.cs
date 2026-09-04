using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Adds the scheduler to the consume context, so that it can be used for message redelivery
/// </summary>
public class PublishMessageSchedulerFilter :
    IFilter<ConsumeContext>
{
    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scheduler");
        scope.Add("type", "publish");
    }

    [DebuggerNonUserCode]
    Task IFilter<ConsumeContext>.SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        context.GetOrAddPayload<MessageSchedulerContext>(() => new ConsumeMessageSchedulerContext(context, SchedulerFactory));

        return next.SendAsync(context);
    }

    static IMessageScheduler SchedulerFactory(ConsumeContext context)
    {
        return new MessageScheduler(new PublishScheduleMessageProvider(context), context.GetPayload<IBusTopology>(), context.GetTimeProvider());
    }
}
