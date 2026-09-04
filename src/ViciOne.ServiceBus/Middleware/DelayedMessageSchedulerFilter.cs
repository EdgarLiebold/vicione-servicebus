using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Middleware;

public class DelayedMessageSchedulerFilter :
    IFilter<ConsumeContext>
{
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("delayedMessageScheduler");
    }

    [DebuggerNonUserCode]
    public Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        context.GetOrAddPayload<MessageSchedulerContext>(() => new ConsumeMessageSchedulerContext(context, SchedulerFactory));

        return next.SendAsync(context);
    }

    static IMessageScheduler SchedulerFactory(ConsumeContext context)
    {
        TimeProvider timeProvider = context.GetTimeProvider();
        return new MessageScheduler(new DelayedScheduleMessageProvider(context, timeProvider), context.GetPayload<IBusTopology>(), timeProvider);
    }
}
