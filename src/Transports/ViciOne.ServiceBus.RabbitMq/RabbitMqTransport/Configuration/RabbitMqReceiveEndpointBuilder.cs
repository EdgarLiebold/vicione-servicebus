using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq receive endpoint builder implementation.
/// </summary>
public class RabbitMqReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IRabbitMqReceiveEndpointConfiguration _configuration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    public RabbitMqReceiveEndpointBuilder(IRabbitMqHostConfiguration hostConfiguration, IRabbitMqReceiveEndpointConfiguration configuration)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public override ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
    {
        if (_configuration.ConfigureConsumeTopology && options.HasFlag(ConnectPipeOptions.ConfigureConsumeTopology))
        {
            IRabbitMqMessageConsumeTopologyConfigurator<T> topology = _configuration.Topology.Consume.GetMessageTopology<T>();
            if (topology.ConfigureConsumeTopology)
                topology.Bind();
        }

        return base.ConnectConsumePipe(pipe, options);
    }

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public RabbitMqReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var brokerTopology = BuildTopology(_configuration.Settings);

        var deadLetterTransport = CreateDeadLetterTransport();
        var errorTransport = CreateErrorTransport();

        var context = new RabbitMqQueueReceiveEndpointContext(_hostConfiguration, _configuration, brokerTopology);

        context.GetOrAddPayload(() => deadLetterTransport);
        context.GetOrAddPayload(() => errorTransport);
        context.GetOrAddPayload(() => _hostConfiguration.Topology);

        return context;
    }

    IErrorTransport CreateErrorTransport()
    {
        var errorSettings = _configuration.Topology.Send.GetErrorSettings(_configuration.Settings);
        var filter = new ConfigureRabbitMqTopologyFilter<ErrorSettings>(errorSettings, errorSettings.GetBrokerTopology());

        return new RabbitMqErrorTransport(errorSettings.ExchangeName, filter);
    }

    IDeadLetterTransport CreateDeadLetterTransport()
    {
        var deadLetterSettings = _configuration.Topology.Send.GetDeadLetterSettings(_configuration.Settings);
        var filter = new ConfigureRabbitMqTopologyFilter<DeadLetterSettings>(deadLetterSettings, deadLetterSettings.GetBrokerTopology());

        return new RabbitMqDeadLetterTransport(deadLetterSettings.ExchangeName, filter);
    }

    BrokerTopology BuildTopology(ReceiveSettings settings)
    {
        var topologyBuilder = new ReceiveEndpointBrokerTopologyBuilder();

        if (settings.QueueName.Equals(RabbitMqExchangeNames.ReplyTo, StringComparison.OrdinalIgnoreCase))
            return topologyBuilder.BuildBrokerTopology();

        var queueArguments = new Dictionary<string, object?>(settings.QueueArguments);

        if (settings.QueueExpiration.HasValue)
            queueArguments[RabbitMQ.Client.Headers.XExpires] = (long)settings.QueueExpiration.Value.TotalMilliseconds;

        topologyBuilder.Exchange = topologyBuilder.ExchangeDeclare(settings.ExchangeName ?? settings.QueueName, settings.ExchangeType, settings.Durable,
            settings.AutoDelete, settings.ExchangeArguments);

        if (settings.BindQueue)
        {
            topologyBuilder.Queue = topologyBuilder.QueueDeclare(settings.QueueName, settings.Durable, settings.AutoDelete, settings.Exclusive,
                queueArguments);

            topologyBuilder.QueueBind(topologyBuilder.Exchange, topologyBuilder.Queue, settings.RoutingKey, settings.BindingArguments);
        }

        _configuration.Topology.Consume.Apply(topologyBuilder);

        return topologyBuilder.BuildBrokerTopology();
    }
}
