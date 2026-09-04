using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a queue sql receive endpoint context implementation.
/// </summary>
public class QueueSqlReceiveEndpointContext :
    BaseReceiveEndpointContext,
    SqlReceiveEndpointContext
{
    readonly Recycle<IClientContextSupervisor> _clientContext;
    readonly ISqlReceiveEndpointConfiguration _configuration;
    readonly ISqlHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    public QueueSqlReceiveEndpointContext(ISqlHostConfiguration hostConfiguration, ISqlReceiveEndpointConfiguration configuration,
        BrokerTopology brokerTopology)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;

        BrokerTopology = brokerTopology;

        _clientContext = new Recycle<IClientContextSupervisor>(() => new ClientContextSupervisor(_hostConfiguration.ConnectionContextSupervisor));
    }

    /// <summary>
    /// Gets the client context supervisor value.
    /// </summary>
    public IClientContextSupervisor ClientContextSupervisor => _clientContext.Supervisor;

    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>
    /// Performs the convert exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new ConnectionException(message + _hostConfiguration.HostAddress, exception);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
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

    /// <summary>
    /// Creates send transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new SqlSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>
    /// Creates publish transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new SqlPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
