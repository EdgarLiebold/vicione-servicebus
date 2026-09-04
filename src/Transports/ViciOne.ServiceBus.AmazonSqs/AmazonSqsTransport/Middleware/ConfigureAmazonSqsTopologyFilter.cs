using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Middleware;
/// <summary>
/// Configures the broker with the supplied topology once the model is created, to ensure
/// that the exchanges, queues, and bindings for the model are properly configured in AmazonSQS.
/// </summary>
public class ConfigureAmazonSqsTopologyFilter<TSettings> :
    IFilter<ClientContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly SqsReceiveEndpointContext? _context;
    readonly TSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    /// <param name="context">The operation context.</param>
    public ConfigureAmazonSqsTopologyFilter(TSettings settings, BrokerTopology brokerTopology, SqsReceiveEndpointContext? context = null)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
        _context = context;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ClientContext context, IPipe<ClientContext> next)
    {
        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext = await ConfigureAsync(context, context.CancellationToken);

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
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<OneTimeContext<ConfigureTopologyContext<TSettings>>> ConfigureAsync(ClientContext context, CancellationToken cancellationToken)
    {
        return await context.OneTimeSetupAsync<ConfigureTopologyContext<TSettings>>(() =>
        {
            context.GetOrAddPayload(() => _settings);

            if (_context != null && AnyAutoDelete())
                _context.AddSendAgent(new RemoveAmazonSqsTopologyAgent(context, _brokerTopology));

            return ConfigureTopologyAsync(context, cancellationToken);
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    async Task ConfigureTopologyAsync(ClientContext context, CancellationToken cancellationToken)
    {
        IEnumerable<Task<TopicInfo>> topics = _brokerTopology.Topics.Select(topic => DeclareAsync(context, topic, cancellationToken));

        IEnumerable<Task<QueueInfo>> queues = _brokerTopology.Queues.Select(queue => DeclareAsync(context, queue, cancellationToken));

        await Task.WhenAll(topics).ConfigureAwait(false);
        await Task.WhenAll(queues).ConfigureAwait(false);

        IEnumerable<Task> subscriptions = _brokerTopology.QueueSubscriptions.Select(subscription => DeclareAsync(context, subscription, cancellationToken));
        await Task.WhenAll(subscriptions).ConfigureAwait(false);
    }

    bool AnyAutoDelete()
    {
        return _brokerTopology.Topics.Any(x => x.AutoDelete) || _brokerTopology.Queues.Any(x => x.AutoDelete);
    }

    internal static async Task<TopicInfo> DeclareAsync(ClientContext context, Topic topic, CancellationToken cancellationToken)
    {
        var topicInfo = await context.CreateTopicAsync(topic, cancellationToken).ConfigureAwait(false);

        if (topicInfo.Existing)
        {
            LogContext.Debug?.Log("Existing topic {Topic} {TopicArn}", topicInfo.EntityName, topicInfo.Arn);
            return topicInfo;
        }

        LogContext.Debug?.Log("Created topic {Topic} {TopicArn}", topicInfo.EntityName, topicInfo.Arn);

        return topicInfo;
    }

    static async Task DeclareAsync(ClientContext context, QueueSubscription subscription, CancellationToken cancellationToken)
    {
        var created = await context.CreateQueueSubscriptionAsync(subscription.Source, subscription.Destination, cancellationToken).ConfigureAwait(false);
        LogContext.Debug?.Log(created ? "Created subscription {Topic} to {Queue}" : "Existing subscription {Topic} to {Queue}",
            subscription.Source, subscription.Destination);
    }

    internal static async Task<QueueInfo> DeclareAsync(ClientContext context, Queue queue, CancellationToken cancellationToken)
    {
        var queueInfo = await context.CreateQueueAsync(queue, cancellationToken).ConfigureAwait(false);
        if (queueInfo.Existing)
        {
            LogContext.Debug?.Log("Existing queue {Queue} {QueueArn} {QueueUrl}", queueInfo.EntityName, queueInfo.Arn, queueInfo.Url);
            return queueInfo;
        }

        LogContext.Debug?.Log("Created queue {Queue} {QueueArn} {QueueUrl}", queueInfo.EntityName, queueInfo.Arn, queueInfo.Url);

        return queueInfo;
    }
}
