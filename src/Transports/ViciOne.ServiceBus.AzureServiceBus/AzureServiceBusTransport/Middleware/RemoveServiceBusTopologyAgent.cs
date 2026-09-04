using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>
/// Provides a remove service bus topology agent implementation.
/// </summary>
public sealed class RemoveServiceBusTopologyAgent :
    Agent
{
    readonly BrokerTopology _brokerTopology;
    readonly ConnectionContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    public RemoveServiceBusTopologyAgent(ConnectionContext context, BrokerTopology brokerTopology)
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
            await RemoveSubscriptionsAsync(_context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogContext.Warning?.Log(ex, "Failed to remove one or more subscriptions from the endpoint.");
        }

        await base.StopAgentAsync(context);
    }

    async Task RemoveSubscriptionsAsync(ConnectionContext context)
    {
        await Task.WhenAll(_brokerTopology.QueueSubscriptions.Select(subscription => DeleteAsync(context, subscription))).ConfigureAwait(false);
    }

    static Task DeleteAsync(ConnectionContext context, QueueSubscription subscription)
    {
        return context.DeleteTopicSubscriptionAsync(subscription.Subscription.CreateSubscriptionOptions, context.CancellationToken);
    }
}
