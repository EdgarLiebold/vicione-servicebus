using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMq.Middleware;

/// <summary>
/// Provides a configure rabbit mq queue redelivery filter implementation.
/// </summary>
public sealed class ConfigureRabbitMqQueueRedeliveryFilter : IFilter<ChannelContext>
{
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="plan">The plan value.</param>
    public ConfigureRabbitMqQueueRedeliveryFilter(RabbitMqQueueRedeliveryPlan plan)
    {
        _plan = plan;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("rabbitMqQueueRedeliveryTopology");
        scope.Add("queue", _plan.QueueName);
        scope.Add("intervals", _plan.Intervals.Count);
    }
}
