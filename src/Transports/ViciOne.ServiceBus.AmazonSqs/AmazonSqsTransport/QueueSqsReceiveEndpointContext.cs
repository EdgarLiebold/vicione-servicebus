using System;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Provides Amazon SQS-specific runtime services for a queue receive endpoint.</summary>
public class QueueSqsReceiveEndpointContext :
    BaseReceiveEndpointContext,
    SqsReceiveEndpointContext
{
    readonly Recycle<IClientContextSupervisor> _clientContext;
    readonly IAmazonSqsReceiveEndpointConfiguration _configuration;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    /// <summary>Initializes an Amazon SQS queue receive-endpoint context.</summary>
    /// <param name="hostConfiguration">The owning host configuration.</param>
    /// <param name="configuration">The receive-endpoint configuration.</param>
    /// <param name="brokerTopology">The endpoint's queue, topics, and subscriptions.</param>
    public QueueSqsReceiveEndpointContext(IAmazonSqsHostConfiguration hostConfiguration, IAmazonSqsReceiveEndpointConfiguration configuration,
        BrokerTopology brokerTopology)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
        BrokerTopology = brokerTopology;

        _clientContext = new Recycle<IClientContextSupervisor>(() => new ClientContextSupervisor(_hostConfiguration.ConnectionContextSupervisor));
    }

    /// <summary>Gets the endpoint's Amazon topology.</summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>Gets the recyclable Amazon client-context supervisor.</summary>
    public IClientContextSupervisor ClientContextSupervisor => _clientContext.Supervisor;

    /// <summary>Adds a send agent to the endpoint's client supervisor.</summary>
    /// <param name="agent">The send agent.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>Adds a consume agent to the endpoint's client supervisor.</summary>
    /// <param name="agent">The consume agent.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _clientContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>Wraps a transport failure with the configured Amazon host identity.</summary>
    /// <param name="exception">The underlying failure.</param>
    /// <param name="message">The contextual error message.</param>
    /// <returns>The Amazon SQS connection exception.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new AmazonSqsConnectionException(message + _hostConfiguration.Settings, exception);
    }

    /// <summary>Adds queue, concurrency, polling, and topology settings to a diagnostic probe.</summary>
    /// <param name="context">The probe context.</param>
    public override void Probe(ProbeContext context)
    {
        context.Add("type", "AmazonSQS");
        context.Set(new
        {
            _configuration.Settings.EntityName,
            _configuration.Settings.Durable,
            _configuration.Settings.AutoDelete,
            _configuration.Settings.PrefetchCount,
            ConcurrentMessageLimit,
            _configuration.Settings.WaitTimeSeconds,
            _configuration.Settings.PurgeOnStartup
        });

        var topologyScope = context.CreateScope("topology");
        BrokerTopology.Probe(topologyScope);
    }

    /// <summary>Creates the endpoint's Amazon SQS send-transport provider.</summary>
    /// <returns>The send-transport provider.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new AmazonSqsSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>Creates the endpoint's Amazon SNS publish-transport provider.</summary>
    /// <returns>The publish-transport provider.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new AmazonSqsPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
