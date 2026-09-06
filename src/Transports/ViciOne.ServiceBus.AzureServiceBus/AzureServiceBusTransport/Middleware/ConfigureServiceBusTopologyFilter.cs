using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Deploys Azure Service Bus broker topology once per client context before the pipeline continues.</summary>
/// <typeparam name="TSettings">The entity settings attached to the one-time configuration context.</typeparam>
public class ConfigureServiceBusTopologyFilter<TSettings> :
    IFilter<ClientContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly ServiceBusReceiveEndpointContext? _context;
    readonly bool _removeSubscriptions;
    readonly TSettings _settings;

    /// <summary>Initializes topology deployment and optional subscription cleanup.</summary>
    /// <param name="settings">The entity settings exposed during deployment.</param>
    /// <param name="brokerTopology">The broker entities to create or reconcile.</param>
    /// <param name="removeSubscriptions">Whether queue-forwarding subscriptions are removed when the endpoint stops.</param>
    /// <param name="context">The receive endpoint that owns the cleanup agent, when applicable.</param>
    public ConfigureServiceBusTopologyFilter(TSettings settings, BrokerTopology brokerTopology, bool removeSubscriptions = false,
        ServiceBusReceiveEndpointContext? context = null)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
        _removeSubscriptions = removeSubscriptions;
        _context = context;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe section to populate.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    /// <summary>Ensures topology is configured, then executes the remaining client-context pipeline.</summary>
    /// <param name="context">The client context whose namespace receives the topology.</param>
    /// <param name="next">The remaining pipeline.</param>
    /// <returns>The continuation task returned by <paramref name="next"/> after all configured entities exist.</returns>
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

    /// <summary>Runs entity deployment once for the namespace context and returns its eviction handle.</summary>
    /// <param name="context">The namespace context used for administration operations.</param>
    /// <param name="cancellationToken">The token that cancels topology deployment.</param>
    /// <returns>A task that produces the one-time setup context.</returns>
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
