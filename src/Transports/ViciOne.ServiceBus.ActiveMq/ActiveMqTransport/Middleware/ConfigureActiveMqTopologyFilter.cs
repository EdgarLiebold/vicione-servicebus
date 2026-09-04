using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.ActiveMq.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Middleware;
/// <summary>
/// Configures the broker with the supplied topology once the model is created, to ensure
/// that the exchanges, queues, and bindings for the model are properly configured in ActiveMQ.
/// </summary>
public class ConfigureActiveMqTopologyFilter<TSettings> :
    IFilter<SessionContext>
    where TSettings : class
{
    readonly BrokerTopology _brokerTopology;
    readonly ActiveMqReceiveEndpointContext _context;
    readonly TSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    /// <param name="context">The operation context.</param>
    public ConfigureActiveMqTopologyFilter(TSettings settings, BrokerTopology brokerTopology, ActiveMqReceiveEndpointContext context)
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

        // The outcome, not the steps. Resolving a name is a client side act that leaves the broker
        // without the topic, which is what this filter used to do and why a deployed topology was
        // deployed nowhere.
        return context.EnsureTopicExistsAsync(topic);
    }

    Task DeclareAsync(SessionContext context, Queue queue)
    {
        LogContext.Debug?.Log("Get queue {Queue}", queue);

        return context.GetQueueAsync(queue);
    }
}
