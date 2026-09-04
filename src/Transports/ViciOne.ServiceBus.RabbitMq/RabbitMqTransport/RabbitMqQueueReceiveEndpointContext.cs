using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq queue receive endpoint context implementation.
/// </summary>
public class RabbitMqQueueReceiveEndpointContext :
    BaseReceiveEndpointContext,
    RabbitMqReceiveEndpointContext
{
    readonly IRabbitMqReceiveEndpointConfiguration _configuration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly Recycle<IChannelContextSupervisor> _channelContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="brokerTopology">The broker topology value.</param>
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

    /// <summary>
    /// Gets the broker topology value.
    /// </summary>
    public BrokerTopology BrokerTopology { get; }

    /// <summary>
    /// Gets the exclusive consumer value.
    /// </summary>
    public bool ExclusiveConsumer { get; }
    /// <summary>
    /// Gets the is not reply to value.
    /// </summary>
    public bool IsNotReplyTo { get; }

    /// <summary>
    /// Gets the channel context supervisor value.
    /// </summary>
    public IChannelContextSupervisor ChannelContextSupervisor => _channelContext.Supervisor;

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddSendAgent(IAgent agent)
    {
        _channelContext.Supervisor.AddSendAgent(agent);
    }

    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        _channelContext.Supervisor.AddConsumeAgent(agent);
    }

    /// <summary>
    /// Performs the convert exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return new RabbitMqConnectionException(message + _hostConfiguration.Settings.ToDescription(), exception);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public override void Probe(ProbeContext context)
    {
        context.Add("type", "RabbitMQ");
        if (ConcurrentMessageLimit.HasValue)
            context.Add("concurrentMessageLimit", ConcurrentMessageLimit.Value);
        context.Set(_configuration.Settings);

        var topologyScope = context.CreateScope("topology");
        BrokerTopology.Probe(topologyScope);
    }

    /// <summary>
    /// Creates send transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new RabbitMqSendTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }

    /// <summary>
    /// Creates publish transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new RabbitMqPublishTransportProvider(_hostConfiguration.ConnectionContextSupervisor, this);
    }
}
