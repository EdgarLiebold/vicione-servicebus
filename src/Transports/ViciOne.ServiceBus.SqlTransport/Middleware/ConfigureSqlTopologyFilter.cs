using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Middleware;
/// <summary>Materializes the configured SQL queues, topics, and subscriptions before the client pipeline runs.</summary>
/// <typeparam name="TSettings">The settings type.</typeparam>
public class ConfigureSqlTopologyFilter<TSettings> :
    IFilter<ClientContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly SqlReceiveEndpointContext? _context;
    readonly TSettings _settings;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    /// <param name="brokerTopology">The broker topology.</param>
    /// <param name="context">The context associated with the operation.</param>
    public ConfigureSqlTopologyFilter(TSettings settings, BrokerTopology brokerTopology, SqlReceiveEndpointContext? context = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _brokerTopology = brokerTopology ?? throw new ArgumentNullException(nameof(brokerTopology));
        _context = context;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ClientContext context, IPipe<ClientContext> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        OneTimeContext<TopologySetup> oneTimeContext = await context.OneTimeSetupAsync<TopologySetup>(() =>
        {
            context.GetOrAddPayload(() => _settings);

            return ConfigureTopologyAsync(context);
        }).ConfigureAwait(false);

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

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    async Task ConfigureTopologyAsync(ClientContext context)
    {
        foreach (var queue in _brokerTopology.Queues)
        {
            var queueId = await CreateQueueAsync(context, queue).ConfigureAwait(false);
            if (queue.QueueName == _context?.InputAddress.GetEndpointName() && _settings is SqlReceiveSettings settings)
                settings.QueueId = queueId;
        }

        foreach (var topic in _brokerTopology.Topics)
            await CreateTopicAsync(context, topic).ConfigureAwait(false);

        foreach (var topicSubscription in _brokerTopology.TopicSubscriptions)
            await CreateTopicSubscriptionAsync(context, topicSubscription).ConfigureAwait(false);

        foreach (var queueSubscription in _brokerTopology.QueueSubscriptions)
            await CreateQueueSubscriptionAsync(context, queueSubscription).ConfigureAwait(false);
    }

    static Task CreateTopicAsync(ClientContext context, Topic topic)
    {
        SqlLogMessages.CreateTopic(topic);

        return context.CreateTopicAsync(topic);
    }

    static Task CreateQueueSubscriptionAsync(ClientContext context, TopicToQueueSubscription subscription)
    {
        SqlLogMessages.CreateQueueSubscription(subscription);

        return context.CreateQueueSubscriptionAsync(subscription);
    }

    static Task CreateTopicSubscriptionAsync(ClientContext context, TopicToTopicSubscription subscription)
    {
        SqlLogMessages.CreateTopicSubscription(subscription);

        return context.CreateTopicSubscriptionAsync(subscription);
    }

    static Task<long> CreateQueueAsync(ClientContext context, Queue queue)
    {
        SqlLogMessages.CreateQueue(queue);

        return context.CreateQueueAsync(queue);
    }

    sealed class TopologySetup
    {
    }
}
