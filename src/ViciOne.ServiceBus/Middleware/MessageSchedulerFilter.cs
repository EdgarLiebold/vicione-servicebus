using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Adds the scheduler to the consume context, so that it can be used for message redelivery
/// </summary>
public class MessageSchedulerFilter :
    IFilter<ConsumeContext>
{
    readonly Uri _schedulerAddress;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedulerAddress">The scheduler address value.</param>
    public MessageSchedulerFilter(Uri schedulerAddress)
    {
        _schedulerAddress = schedulerAddress;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("scheduler");
        scope.Add("type", "send");
        scope.Add("address", _schedulerAddress);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ConsumeContext context, IPipe<ConsumeContext> next)
    {
        context.GetOrAddPayload<MessageSchedulerContext>(() => new ConsumeMessageSchedulerContext(context, SchedulerFactory));

        return next.SendAsync(context);
    }

    IMessageScheduler SchedulerFactory(ConsumeContext context)
    {
        return new MessageScheduler(new EndpointScheduleMessageProvider(() => context.GetSendEndpointAsync(_schedulerAddress)),
            context.GetPayload<IBusTopology>(), context.GetTimeProvider());
    }
}
