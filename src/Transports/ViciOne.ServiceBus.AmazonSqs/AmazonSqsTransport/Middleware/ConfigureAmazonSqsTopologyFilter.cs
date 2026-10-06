using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Middleware;
/// <summary>
/// Configures Amazon SNS topics, Amazon SQS queues, and their subscriptions once per client context.
/// </summary>
/// <typeparam name="TSettings">The entity settings exposed to subsequent pipeline stages.</typeparam>
public class ConfigureAmazonSqsTopologyFilter<TSettings> :
    IFilter<ClientContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly SqsReceiveEndpointContext? _context;
    readonly TSettings _settings;
    readonly object _autoDeleteAgentLock = new();
    RemoveAmazonSqsTopologyAgent? _autoDeleteAgent;

    /// <summary>Initializes a topology-configuration filter.</summary>
    /// <param name="settings">The entity settings added to the client context.</param>
    /// <param name="brokerTopology">The topics, queues, and subscriptions to declare.</param>
    /// <param name="context">The optional receive endpoint that owns automatic topology removal.</param>
    public ConfigureAmazonSqsTopologyFilter(TSettings settings, BrokerTopology brokerTopology, SqsReceiveEndpointContext? context = null)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
        _context = context;
    }

    /// <summary>Declares topology once, invokes the next client-context stage, and evicts failed setup state for retry.</summary>
    /// <param name="context">The Amazon client context.</param>
    /// <param name="next">The next pipeline stage.</param>
    /// <returns>The continuation task returned by <paramref name="next"/> after topology is ready.</returns>
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

    /// <summary>Adds the configured broker topology to a diagnostic probe.</summary>
    /// <param name="context">The probe context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    /// <summary>Declares topology once for a client context and registers automatic cleanup when required.</summary>
    /// <param name="context">The Amazon client context.</param>
    /// <param name="cancellationToken">The token used to cancel entity declaration.</param>
    /// <returns>The one-time setup handle, which can be evicted after a downstream failure.</returns>
    public async Task<OneTimeContext<ConfigureTopologyContext<TSettings>>> ConfigureAsync(ClientContext context, CancellationToken cancellationToken)
    {
        return await context.OneTimeSetupAsync<ConfigureTopologyContext<TSettings>>(() =>
        {
            context.GetOrAddPayload(() => _settings);

            if (_context != null && AnyAutoDelete())
                RegisterAutoDeleteAgent(context);

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

    void RegisterAutoDeleteAgent(ClientContext context)
    {
        lock (_autoDeleteAgentLock)
        {
            while (context is ScopeClientContext or SharedClientContext)
            {
                context = context switch
                {
                    ScopeClientContext scope => scope.ParentClientContext,
                    SharedClientContext shared => shared.ParentClientContext,
                    _ => throw new InvalidOperationException("The client context cannot be unwrapped.")
                };
            }

            if (_autoDeleteAgent is { } current && current.TryUpdateContext(context))
                return;

            var agent = new RemoveAmazonSqsTopologyAgent(context, _brokerTopology);
            _context!.AddSendAgent(agent);
            _autoDeleteAgent = agent;
        }
    }

    internal static async Task<TopicInfo> DeclareAsync(ClientContext context, Topic topic, CancellationToken cancellationToken)
    {
        var topicInfo = await context.CreateTopicAsync(topic, cancellationToken).ConfigureAwait(false);

        if (topicInfo.Existing)
        {
            try
            {
                LogContext.Debug?.Log("Existing topic {Topic} {TopicArn}", topicInfo.EntityName, topicInfo.Arn);
            }
            catch (Exception)
            {
            }
            return topicInfo;
        }

        try
        {
            LogContext.Debug?.Log("Created topic {Topic} {TopicArn}", topicInfo.EntityName, topicInfo.Arn);
        }
        catch (Exception)
        {
        }

        return topicInfo;
    }

    static async Task DeclareAsync(ClientContext context, QueueSubscription subscription, CancellationToken cancellationToken)
    {
        var created = await context.CreateQueueSubscriptionAsync(subscription.Source, subscription.Destination, cancellationToken).ConfigureAwait(false);
        try
        {
            LogContext.Debug?.Log(created ? "Created subscription {Topic} to {Queue}" : "Existing subscription {Topic} to {Queue}",
                subscription.Source, subscription.Destination);
        }
        catch (Exception)
        {
        }
    }

    internal static async Task<QueueInfo> DeclareAsync(ClientContext context, Queue queue, CancellationToken cancellationToken)
    {
        var queueInfo = await context.CreateQueueAsync(queue, cancellationToken).ConfigureAwait(false);
        if (queueInfo.Existing)
        {
            try
            {
                LogContext.Debug?.Log("Existing queue {Queue} {QueueArn} {QueueUrl}", queueInfo.EntityName, queueInfo.Arn, queueInfo.Url);
            }
            catch (Exception)
            {
            }
            return queueInfo;
        }

        try
        {
            LogContext.Debug?.Log("Created queue {Queue} {QueueArn} {QueueUrl}", queueInfo.EntityName, queueInfo.Arn, queueInfo.Url);
        }
        catch (Exception)
        {
        }

        return queueInfo;
    }
}
