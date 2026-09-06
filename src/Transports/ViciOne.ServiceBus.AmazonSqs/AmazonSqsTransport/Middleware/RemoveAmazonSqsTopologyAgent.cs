using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs.Middleware;

/// <summary>Deletes auto-delete Amazon SQS queues and Amazon SNS topics when its endpoint stops.</summary>
public sealed class RemoveAmazonSqsTopologyAgent :
    Agent
{
    readonly BrokerTopology _brokerTopology;
    readonly ClientContext _context;

    /// <summary>Initializes a topology-removal agent in the ready state.</summary>
    /// <param name="context">The client context used for entity deletion.</param>
    /// <param name="brokerTopology">The topology whose auto-delete entities are owned by the endpoint.</param>
    public RemoveAmazonSqsTopologyAgent(ClientContext context, BrokerTopology brokerTopology)
    {
        _brokerTopology = brokerTopology;
        _context = context;

        SetReady();
    }

    /// <summary>Deletes auto-delete entities and then stops the agent.</summary>
    /// <param name="context">The stop context whose token cancels topology deletion.</param>
    /// <returns>A task that completes when deletion has been attempted and the agent has stopped.</returns>
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
