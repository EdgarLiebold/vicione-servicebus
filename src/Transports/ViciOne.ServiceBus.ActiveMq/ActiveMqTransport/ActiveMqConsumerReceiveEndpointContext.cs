using System;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Provides the runtime context for an ActiveMQ consumer receive endpoint.</summary>
public class ActiveMqConsumerReceiveEndpointContext :
    BaseReceiveEndpointContext,
    ActiveMqReceiveEndpointContext
{
    readonly IActiveMqReceiveEndpointConfiguration _configuration;
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly Recycle<ISessionContextSupervisor> _sessionContext;

    /// <summary>Creates a receive-endpoint context for the configured broker topology.</summary>
    /// <param name="hostConfiguration">The ActiveMQ host configuration.</param>
    /// <param name="configuration">The receive-endpoint configuration.</param>
    /// <param name="brokerTopology">The topology deployed for the endpoint.</param>
    public ActiveMqConsumerReceiveEndpointContext(IActiveMqHostConfiguration hostConfiguration, IActiveMqReceiveEndpointConfiguration configuration,
        BrokerTopology brokerTopology)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
        BrokerTopology = brokerTopology;

        _sessionContext = new Recycle<ISessionContextSupervisor>(() => new SessionContextSupervisor(hostConfiguration.ConnectionContextSupervisor));
    }

    /// <summary>Gets the topology deployed for the endpoint.</summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>Gets the supervisor for the endpoint's broker connection.</summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _hostConfiguration.ConnectionContextSupervisor;

    /// <summary>Gets the recyclable supervisor for the endpoint's Apache NMS session.</summary>
    public ISessionContextSupervisor SessionContextSupervisor => _sessionContext.Supervisor;

    /// <summary>Adds a send agent to the endpoint session supervisor.</summary>
    /// <param name="agent">The send agent to supervise.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _sessionContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>Adds a consume agent to the endpoint session supervisor.</summary>
    /// <param name="agent">The consume agent to supervise.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _sessionContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>Wraps a failure with the configured ActiveMQ host description.</summary>
    /// <param name="exception">The underlying failure.</param>
    /// <param name="message">The contextual error message.</param>
    /// <returns>An ActiveMQ connection exception.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new ActiveMqConnectionException(message + _hostConfiguration.Settings.ToDescription(), exception);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context to populate.</param>
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

    /// <summary>Creates the endpoint's ActiveMQ send-transport provider.</summary>
    /// <returns>A send-transport provider bound to this endpoint.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new ActiveMqSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>Creates the endpoint's ActiveMQ publish-transport provider.</summary>
    /// <returns>A publish-transport provider bound to this endpoint.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new ActiveMqPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
