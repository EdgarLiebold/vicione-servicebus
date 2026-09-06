using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Middleware;
/// <summary>
/// Resolves the topics and queues required by a broker topology once per Apache NMS session context.
/// </summary>
/// <typeparam name="TSettings">The settings associated with the topology deployment.</typeparam>
public class ConfigureActiveMqTopologyFilter<TSettings> :
    IFilter<SessionContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly ActiveMqReceiveEndpointContext _context;
    readonly TSettings _settings;

    /// <summary>Creates a one-time topology-deployment filter.</summary>
    /// <param name="settings">The settings associated with the deployment.</param>
    /// <param name="brokerTopology">The topics and queues to resolve.</param>
    /// <param name="context">The receive endpoint that owns cleanup agents.</param>
    public ConfigureActiveMqTopologyFilter(TSettings settings, BrokerTopology brokerTopology, ActiveMqReceiveEndpointContext context)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
        _context = context;
    }

    /// <summary>Ensures the topology exists, executes the next session stage, and installs OpenWire cleanup when needed.</summary>
    /// <param name="context">The Apache NMS session context.</param>
    /// <param name="next">The next session pipeline stage.</param>
    /// <returns>A task that completes when the next stage completes.</returns>
    public async Task SendAsync(SessionContext context, IPipe<SessionContext> next)
    {
        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext = await ConfigureAsync(context);

        try
        {
            await next.SendAsync(context).ConfigureAwait(false);

            // Apache.NMS.ActiveMQ exposes explicit destination deletion, whereas Apache.NMS.AMQP
            // deliberately does not. AMQP brokers own auto-delete lifetime and reject a client-side
            // DeleteDestination call. Install the manual cleanup owner only for the provider that
            // can fulfill that contract; unexpected OpenWire cleanup failures remain observable.
            if (_settings is ReceiveSettings && RequiresManualAutoDelete(context.ConnectionContext.HostAddress))
                _context.AddSendAgent(new RemoveAutoDeleteAgent(_context.ConnectionContextSupervisor, _brokerTopology));
        }
        catch (Exception)
        {
            oneTimeContext.Evict();

            throw;
        }
    }

    internal static bool RequiresManualAutoDelete(Uri hostAddress)
    {
        ArgumentNullException.ThrowIfNull(hostAddress);

        return string.Equals(hostAddress.Scheme, ActiveMqHostAddress.ActiveMqScheme, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context to populate.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    /// <summary>Ensures the broker topology is configured once for a session context.</summary>
    /// <param name="context">The Apache NMS session context.</param>
    /// <param name="cancellationToken">The token used to cancel one-time setup.</param>
    /// <returns>A task that produces the one-time setup handle.</returns>
    public async Task<OneTimeContext<ConfigureTopologyContext<TSettings>>> ConfigureAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        return await context.OneTimeSetupAsync<ConfigureTopologyContext<TSettings>>(() =>
        {
            context.GetOrAddPayload(() => _settings);

            return ConfigureTopologyAsync(context);
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    async Task ConfigureTopologyAsync(SessionContext context)
    {
        await Task.WhenAll(_brokerTopology.Topics.Select(topic => DeclareAsync(context, topic))).ConfigureAwait(false);

        await Task.WhenAll(_brokerTopology.Queues.Select(queue => DeclareAsync(context, queue))).ConfigureAwait(false);
    }

    Task DeclareAsync(SessionContext context, Topic topic)
    {
        LogContext.Debug?.Log("Declare topic {Topic}", topic);

        // Topology deployment completes only after the broker confirms that the topic exists.
        return context.EnsureTopicExistsAsync(topic);
    }

    Task DeclareAsync(SessionContext context, Queue queue)
    {
        LogContext.Debug?.Log("Get queue {Queue}", queue);

        return context.GetQueueAsync(queue);
    }
}
