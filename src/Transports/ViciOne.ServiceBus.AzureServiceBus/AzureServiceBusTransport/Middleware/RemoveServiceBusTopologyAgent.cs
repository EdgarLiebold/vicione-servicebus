using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Removes queue-forwarding subscriptions when a temporary receive endpoint stops.</summary>
public sealed class RemoveServiceBusTopologyAgent :
    Agent
{
    readonly BrokerTopology _brokerTopology;
    readonly ConnectionContext _context;

    /// <summary>Initializes cleanup for a deployed endpoint topology.</summary>
    /// <param name="context">The namespace connection used for subscription deletion.</param>
    /// <param name="brokerTopology">The topology containing subscriptions to remove.</param>
    public RemoveServiceBusTopologyAgent(ConnectionContext context, BrokerTopology brokerTopology)
    {
        _brokerTopology = brokerTopology;
        _context = context;

        SetReady();
    }

    /// <summary>Attempts to remove all queue-forwarding subscriptions before completing agent shutdown.</summary>
    /// <param name="context">The agent stop context.</param>
    /// <returns>A task that completes after cleanup and base shutdown.</returns>
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
