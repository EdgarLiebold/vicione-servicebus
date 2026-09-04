using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMqTransport.Middleware;

public sealed class ConfigureRabbitMqQueueRedeliveryFilter : IFilter<ChannelContext>
{
    readonly RabbitMqQueueRedeliveryPlan _plan;

    public ConfigureRabbitMqQueueRedeliveryFilter(RabbitMqQueueRedeliveryPlan plan)
    {
        _plan = plan;
    }

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

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("rabbitMqQueueRedeliveryTopology");
        scope.Add("queue", _plan.QueueName);
        scope.Add("intervals", _plan.Intervals.Count);
    }
}
