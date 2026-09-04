using System;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an active mq consumer receive endpoint context implementation.
/// </summary>
public class ActiveMqConsumerReceiveEndpointContext :
    BaseReceiveEndpointContext,
    ActiveMqReceiveEndpointContext
{
    readonly IActiveMqReceiveEndpointConfiguration _configuration;
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly Recycle<ISessionContextSupervisor> _sessionContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
    public ActiveMqConsumerReceiveEndpointContext(IActiveMqHostConfiguration hostConfiguration, IActiveMqReceiveEndpointConfiguration configuration,
        BrokerTopology brokerTopology)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
        BrokerTopology = brokerTopology;

        _sessionContext = new Recycle<ISessionContextSupervisor>(() => new SessionContextSupervisor(hostConfiguration.ConnectionContextSupervisor));
    }

    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _hostConfiguration.ConnectionContextSupervisor;

    /// <summary>
    /// Gets the session context supervisor value.
    /// </summary>
    public ISessionContextSupervisor SessionContextSupervisor => _sessionContext.Supervisor;

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _sessionContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _sessionContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>
    /// Performs the convert exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new ActiveMqConnectionException(message + _hostConfiguration.Settings.ToDescription(), exception);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public override void Probe(ProbeContext context)
    {
        context.Add("type", "ActiveMQ");
        context.Set(new
        {
            _configuration.Settings.EntityName,
            _configuration.Settings.Durable,
            _configuration.Settings.AutoDelete,
            _configuration.Settings.PrefetchCount,
            _configuration.Settings.ConcurrentMessageLimit
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
        return new ActiveMqSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>
    /// Creates publish transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new ActiveMqPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
