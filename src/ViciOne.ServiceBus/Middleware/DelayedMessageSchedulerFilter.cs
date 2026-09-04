using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a delayed message scheduler filter implementation.
/// </summary>
public class DelayedMessageSchedulerFilter :
    IFilter<ConsumeContext>
{
    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("delayedMessageScheduler");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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
