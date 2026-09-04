using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AmazonSqs.Middleware;

/// <summary>
/// Provides a remove amazon sqs topology agent implementation.
/// </summary>
public sealed class RemoveAmazonSqsTopologyAgent :
    Agent
{
    readonly BrokerTopology _brokerTopology;
    readonly ClientContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    public RemoveAmazonSqsTopologyAgent(ClientContext context, BrokerTopology brokerTopology)
    {
        _brokerTopology = brokerTopology;
        _context = context;

        SetReady();
    }

    /// <summary>
    /// Stops agent.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
