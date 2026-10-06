using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Carries state for queue sql receive endpoint operations.</summary>
public class QueueSqlReceiveEndpointContext :
    BaseReceiveEndpointContext,
    SqlReceiveEndpointContext
{
    readonly Recycle<IClientContextSupervisor> _clientContext;
    readonly ISqlReceiveEndpointConfiguration _configuration;
    readonly ISqlHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="configuration">The SQL receive-endpoint configuration used by this context.</param>
    /// <param name="brokerTopology">The broker topology.</param>
    public QueueSqlReceiveEndpointContext(ISqlHostConfiguration hostConfiguration, ISqlReceiveEndpointConfiguration configuration,
        BrokerTopology brokerTopology)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;

        BrokerTopology = brokerTopology;

        _clientContext = new Recycle<IClientContextSupervisor>(() => new ClientContextSupervisor(_hostConfiguration.ConnectionContextSupervisor));
    }

    /// <summary>Gets the client context supervisor.</summary>
    public IClientContextSupervisor ClientContextSupervisor => _clientContext.Supervisor;

    /// <summary>Gets the broker topology.</summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>Adds send agent to the configuration.</summary>
    /// <param name="agent">The agent.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>Adds consume agent to the configuration.</summary>
    /// <param name="agent">The agent.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>Converts exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The converted exception.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new ConnectionException(message + _hostConfiguration.HostAddress, exception, isTransient: true);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public override void Probe(ProbeContext context)
    {
        context.Add("type", "Sql");
        context.Set(new
        {
            _configuration.Settings.QueueName,
            _configuration.Settings.AutoDeleteOnIdle,
            _configuration.Settings.PrefetchCount,
            ConcurrentMessageLimit,
            _configuration.Settings.ConcurrentDeliveryLimit,
            _configuration.Settings.ReceiveMode,
            _configuration.Settings.PollingInterval,
            _configuration.Settings.PurgeOnStartup
        });

        var topologyScope = context.CreateScope("topology");
        BrokerTopology.Probe(topologyScope);
    }

    /// <summary>Creates send transport provider.</summary>
    /// <returns>The created send transport provider.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new SqlSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>Creates publish transport provider.</summary>
    /// <returns>The created publish transport provider.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new SqlPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
