using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes delayed message scheduler pipeline stages.</summary>
public class DelayedMessageSchedulerFilter :
    IFilter<ConsumeContext>
{
    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("delayedMessageScheduler");
    }

    /// <summary>Provides a transport-delay message scheduler when absent, then forwards the consume context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
