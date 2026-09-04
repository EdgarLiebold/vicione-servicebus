using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;

public sealed class RemoveServiceBusTopologyAgent :
    Agent
{
    readonly BrokerTopology _brokerTopology;
    readonly ConnectionContext _context;

    public RemoveServiceBusTopologyAgent(ConnectionContext context, BrokerTopology brokerTopology)
    {
        _brokerTopology = brokerTopology;
        _context = context;

        SetReady();
    }

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
