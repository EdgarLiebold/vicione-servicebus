using System;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;

namespace ViciOne.ServiceBus.ActiveMqTransport.Middleware;
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

    public ConfigureActiveMqTopologyFilter(TSettings settings, BrokerTopology brokerTopology, ActiveMqReceiveEndpointContext context)
    {
        _settings = settings;
        _brokerTopology = brokerTopology;
        _context = context;
    }

    public async Task Send(SessionContext context, IPipe<SessionContext> next)
    {
        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext = await Configure(context);

        try
        {
            await next.Send(context).ConfigureAwait(false);

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

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("configureTopology");

        _brokerTopology.Probe(scope);
    }

    public async Task<OneTimeContext<ConfigureTopologyContext<TSettings>>> Configure(SessionContext context)
    {
        return await context.OneTimeSetup<ConfigureTopologyContext<TSettings>>(() =>
        {
            context.GetOrAddPayload(() => _settings);

            return ConfigureTopology(context);
        }).ConfigureAwait(false);
    }

    async Task ConfigureTopology(SessionContext context)
    {
        await Task.WhenAll(_brokerTopology.Topics.Select(topic => Declare(context, topic))).ConfigureAwait(false);

        await Task.WhenAll(_brokerTopology.Queues.Select(queue => Declare(context, queue))).ConfigureAwait(false);
    }

    Task Declare(SessionContext context, Topic topic)
    {
        LogContext.Debug?.Log("Declare topic {Topic}", topic);

        // The outcome, not the steps. Resolving a name is a client side act that leaves the broker
        // without the topic, which is what this filter used to do and why a deployed topology was
        // deployed nowhere.
        return context.EnsureTopicExists(topic);
    }

    Task Declare(SessionContext context, Queue queue)
    {
        LogContext.Debug?.Log("Get queue {Queue}", queue);

        return context.GetQueue(queue);
    }
}
