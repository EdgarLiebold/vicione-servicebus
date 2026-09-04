using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>
/// Provides a configure service bus topology filter implementation.
/// </summary>
/// <typeparam name="TSettings">The t settings type.</typeparam>
public class ConfigureServiceBusTopologyFilter<TSettings> :
    IFilter<ClientContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly ServiceBusReceiveEndpointContext? _context;
    readonly bool _removeSubscriptions;
    readonly TSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    /// <param name="removeSubscriptions">The remove subscriptions value.</param>
    /// <param name="context">The operation context.</param>
    public ConfigureServiceBusTopologyFilter(TSettings settings, BrokerTopology brokerTopology, bool removeSubscriptions = false,
        ServiceBusReceiveEndpointContext? context = null)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
        _removeSubscriptions = removeSubscriptions;
        _context = context;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
