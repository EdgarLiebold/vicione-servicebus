using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqsTransport.Middleware;

public sealed class RemoveAmazonSqsTopologyAgent :
    Agent
{
    readonly BrokerTopology _brokerTopology;
    readonly ClientContext _context;

    public RemoveAmazonSqsTopologyAgent(ClientContext context, BrokerTopology brokerTopology)
    {
        _brokerTopology = brokerTopology;
        _context = context;

        SetReady();
    }

    protected override async Task StopAgentAsync(StopContext context)
    {
        try
        {
            await DeleteAutoDeleteAsync(_context, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogContext.Warning?.Log(ex, "Failed to remove one or more queues, topics, or subscriptions from the endpoint.");
        }

        await base.StopAgentAsync(context);
    }

    async Task DeleteAutoDeleteAsync(ClientContext context, CancellationToken cancellationToken)
    {
        IEnumerable<Task> topics = _brokerTopology.Topics.Where(x => x.AutoDelete).Select(topic => DeleteAsync(context, topic, cancellationToken));

        IEnumerable<Task> queues = _brokerTopology.Queues.Where(x => x.AutoDelete).Select(queue => DeleteAsync(context, queue, cancellationToken));

        await Task.WhenAll(topics.Concat(queues)).ConfigureAwait(false);
    }

    static Task DeleteAsync(ClientContext context, Topic topic, CancellationToken cancellationToken)
    {
        LogContext.Debug?.Log("Delete topic {Topic}", topic);

        return context.DeleteTopicAsync(topic, cancellationToken);
    }

    static Task DeleteAsync(ClientContext context, Queue queue, CancellationToken cancellationToken)
    {
        LogContext.Debug?.Log("Delete queue {Queue}", queue);

        return context.DeleteQueueAsync(queue, cancellationToken);
    }
}
