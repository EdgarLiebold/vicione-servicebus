using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>Declares RabbitMQ redelivery queues before continuing the channel pipeline.</summary>
public sealed class ConfigureRabbitMqQueueRedeliveryFilter : IFilter<ChannelContext>
{
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>Creates the filter for a finite redelivery-queue plan.</summary>
    /// <param name="plan">The RabbitMQ delay queues and routing keys to declare.</param>
    public ConfigureRabbitMqQueueRedeliveryFilter(RabbitMqQueueRedeliveryPlan plan)
    {
        _plan = plan;
    }

    /// <summary>Declares redelivery topology and invalidates the entity cache if configuration fails.</summary>
    /// <param name="context">The active RabbitMQ channel context.</param>
    /// <param name="next">The remainder of the channel pipeline.</param>
    /// <returns>A task that completes when topology configuration and the remaining pipeline complete.</returns>
    public async Task SendAsync(ChannelContext context, IPipe<ChannelContext> next)
    {
        try
        {
            await _plan.ConfigureAsync(context, context.CancellationToken).ConfigureAwait(false);
            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch
        {
            context.ConnectionContext.TopologyEntityCache.Invalidate();
            throw;
        }
    }

    /// <summary>Adds the redelivery queue and interval count to the diagnostic probe.</summary>
    /// <param name="context">The probe context that receives the filter scope.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("rabbitMqQueueRedeliveryTopology");
        scope.Add("queue", _plan.QueueName);
        scope.Add("intervals", _plan.Intervals.Count);
    }
}
