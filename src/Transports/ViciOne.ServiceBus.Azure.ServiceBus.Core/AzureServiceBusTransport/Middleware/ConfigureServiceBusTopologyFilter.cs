using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;

public class ConfigureServiceBusTopologyFilter<TSettings> :
    IFilter<ClientContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly ServiceBusReceiveEndpointContext? _context;
    readonly bool _removeSubscriptions;
    readonly TSettings _settings;

    public ConfigureServiceBusTopologyFilter(TSettings settings, BrokerTopology brokerTopology, bool removeSubscriptions = false,
        ServiceBusReceiveEndpointContext? context = null)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
        _removeSubscriptions = removeSubscriptions;
        _context = context;
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    public async Task SendAsync(ClientContext context, IPipe<ClientContext> next)
    {
        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext = await ConfigureAsync(context, context.CancellationToken).ConfigureAwait(false);

        try
        {
            await next.SendAsync(context).ConfigureAwait(false);
        }
        catch (Exception)
        {
            oneTimeContext.Evict();

            throw;
        }
    }

    public async Task<OneTimeContext<ConfigureTopologyContext<TSettings>>> ConfigureAsync(NamespaceContext context, CancellationToken cancellationToken)
    {
        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext = await context.OneTimeSetupAsync<ConfigureTopologyContext<TSettings>>(() =>
        {
            context.GetOrAddPayload(() => _settings);

            if (_context != null && _removeSubscriptions)
                _context.AddSendAgent(new RemoveServiceBusTopologyAgent(context.ConnectionContext, _brokerTopology));

            return ConfigureTopologyAsync(context.ConnectionContext, cancellationToken);
        }, cancellationToken: cancellationToken).ConfigureAwait(false);

        return oneTimeContext;
    }

    async Task ConfigureTopologyAsync(ConnectionContext context, CancellationToken cancellationToken)
    {
        StartedActivity? activity = LogContext.Current?.StartGenericActivity("Configure Topology");
        try
        {
            await Task.WhenAll(_brokerTopology.Topics.Select(topic => CreateAsync(context, topic, cancellationToken))).ConfigureAwait(false);

            await Task.WhenAll(_brokerTopology.Queues.Select(queue => CreateAsync(context, queue, cancellationToken))).ConfigureAwait(false);

            await Task.WhenAll(_brokerTopology.Subscriptions.Select(subscription => CreateAsync(context, subscription, cancellationToken)))
                .ConfigureAwait(false);

            await Task.WhenAll(_brokerTopology.QueueSubscriptions.Select(subscription => CreateAsync(context, subscription, cancellationToken)))
                .ConfigureAwait(false);

            await Task.WhenAll(_brokerTopology.TopicSubscriptions.Select(subscription => CreateAsync(context, subscription, cancellationToken)))
                .ConfigureAwait(false);
        }
        finally
        {
            activity?.Stop();
        }
    }

    Task CreateAsync(ConnectionContext context, Topic topic, CancellationToken cancellationToken)
    {
        return context.CreateTopicAsync(topic.CreateTopicOptions, cancellationToken);
    }

    Task CreateAsync(ConnectionContext context, Queue queue, CancellationToken cancellationToken)
    {
        return context.CreateQueueAsync(queue.CreateQueueOptions, cancellationToken);
    }

    Task CreateAsync(ConnectionContext context, Subscription subscription, CancellationToken cancellationToken)
    {
        return context.CreateTopicSubscriptionAsync(subscription.CreateSubscriptionOptions, subscription.Rule, subscription.Filter, cancellationToken);
    }

    Task CreateAsync(ConnectionContext context, QueueSubscription subscription, CancellationToken cancellationToken)
    {
        return context.CreateTopicSubscriptionAsync(subscription.Subscription.CreateSubscriptionOptions, subscription.Subscription.Rule,
            subscription.Subscription.Filter, cancellationToken);
    }

    Task DeleteAsync(ConnectionContext context, QueueSubscription subscription, CancellationToken cancellationToken)
    {
        return context.DeleteTopicSubscriptionAsync(subscription.Subscription.CreateSubscriptionOptions, cancellationToken);
    }

    Task CreateAsync(ConnectionContext context, TopicSubscription subscription, CancellationToken cancellationToken)
    {
        return context.CreateTopicSubscriptionAsync(subscription.Subscription.CreateSubscriptionOptions, subscription.Subscription.Rule,
            subscription.Subscription.Filter, cancellationToken);
    }
}
