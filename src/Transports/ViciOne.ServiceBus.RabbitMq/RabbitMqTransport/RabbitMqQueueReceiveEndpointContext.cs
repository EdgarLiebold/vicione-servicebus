using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Owns RabbitMQ receive topology, channel supervision, and transport providers for one queue endpoint.</summary>
public class RabbitMqQueueReceiveEndpointContext :
    BaseReceiveEndpointContext,
    RabbitMqReceiveEndpointContext
{
    readonly IRabbitMqReceiveEndpointConfiguration _configuration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly Recycle<IChannelContextSupervisor> _channelContext;

    /// <summary>Creates a runtime endpoint context from validated host, endpoint, and broker topology.</summary>
    /// <param name="hostConfiguration">The owning RabbitMQ host configuration.</param>
    /// <param name="configuration">The receive-endpoint configuration.</param>
    /// <param name="brokerTopology">The topology deployed for the endpoint.</param>
    public RabbitMqQueueReceiveEndpointContext(IRabbitMqHostConfiguration hostConfiguration, IRabbitMqReceiveEndpointConfiguration configuration,
        BrokerTopology brokerTopology)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;

        ExclusiveConsumer = configuration.Settings.ExclusiveConsumer;
        BrokerTopology = brokerTopology;

        IsNotReplyTo = configuration.Settings.QueueName != RabbitMqExchangeNames.ReplyTo;

        var concurrentMessageLimit = ConcurrentMessageLimit ?? PrefetchCount;
        if (concurrentMessageLimit > ushort.MaxValue)
            concurrentMessageLimit = ushort.MaxValue;

        _channelContext = new Recycle<IChannelContextSupervisor>(() =>
            new ChannelContextSupervisor(hostConfiguration.ConnectionContextSupervisor, (ushort)concurrentMessageLimit));
    }

    /// <summary>Gets the broker topology.</summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>Gets whether the broker permits only this consumer on the queue.</summary>
    public bool ExclusiveConsumer { get; }
    /// <summary>Gets whether the endpoint is a normal queue rather than the direct-reply-to pseudo-queue.</summary>
    public bool IsNotReplyTo { get; }

    /// <summary>Gets the channel context supervisor.</summary>
    public IChannelContextSupervisor ChannelContextSupervisor => _channelContext.Supervisor;

    /// <summary>Adds a send-side dependent to the current channel supervisor.</summary>
    /// <param name="agent">The send transport agent to supervise.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _channelContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>Adds a consume-side dependent to the current channel supervisor.</summary>
    /// <param name="agent">The consumer agent to supervise.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _channelContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>Wraps an endpoint failure with RabbitMQ connection classification and a sanitized host description.</summary>
    /// <param name="exception">The underlying endpoint or client failure.</param>
    /// <param name="message">The contextual failure prefix.</param>
    /// <returns>The RabbitMQ connection exception.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new RabbitMqConnectionException(message + _hostConfiguration.Settings.ToDescription(), exception);
    }

    /// <summary>Adds RabbitMQ endpoint settings and broker topology to the diagnostic probe.</summary>
    /// <param name="context">The probe context that receives endpoint details.</param>
    public override void Probe(ProbeContext context)
    {
        context.Add("type", "RabbitMQ");
        if (ConcurrentMessageLimit.HasValue)
            context.Add("concurrentMessageLimit", ConcurrentMessageLimit.Value);
        context.Set(_configuration.Settings);

        var topologyScope = context.CreateScope("topology");
        BrokerTopology.Probe(topologyScope);
    }

    /// <summary>Creates a send-transport provider bound to this endpoint's channel supervisor.</summary>
    /// <returns>The RabbitMQ send-transport provider.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new RabbitMqSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>Creates a publish-transport provider bound to this endpoint's channel supervisor.</summary>
    /// <returns>The RabbitMQ publish-transport provider.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new RabbitMqPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
