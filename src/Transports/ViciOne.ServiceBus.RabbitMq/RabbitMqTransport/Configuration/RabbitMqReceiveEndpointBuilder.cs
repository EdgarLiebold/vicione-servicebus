using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Builds a RabbitMQ receive pipeline, broker topology, and fault transports.</summary>
public class RabbitMqReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IRabbitMqReceiveEndpointConfiguration _configuration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;

    /// <summary>Creates a builder over validated host and endpoint configuration.</summary>
    /// <param name="hostConfiguration">The RabbitMQ host configuration.</param>
    /// <param name="configuration">The receive-endpoint configuration.</param>
    public RabbitMqReceiveEndpointBuilder(IRabbitMqHostConfiguration hostConfiguration, IRabbitMqReceiveEndpointConfiguration configuration)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>Connects a typed consume pipeline and binds its message topology when requested.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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

    /// <summary>Creates the runtime endpoint context with broker, error, and dead-letter topology.</summary>
    /// <returns>The RabbitMQ receive-endpoint context.</returns>
    public RabbitMqReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateReceiveEndpointContext(null);
    }

    internal RabbitMqReceiveEndpointContext CreateReceiveEndpointContext(Func<IPipe<ConnectionContext>, IPipe<ConnectionContext>>? connectionPipe)
    {
        var brokerTopology = BuildTopology(_configuration.Settings);
        var context = new RabbitMqQueueReceiveEndpointContext(_hostConfiguration, _configuration, brokerTopology, connectionPipe);

        if (_configuration.Settings.QueueName != RabbitMqExchangeNames.ReplyTo)
        {
            context.GetOrAddPayload(CreateDeadLetterTransport);
            context.GetOrAddPayload(CreateErrorTransport);
        }

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

        if (settings.QueueExpiration.HasValue && !queueArguments.ContainsKey(RabbitMQ.Client.Headers.XExpires))
            queueArguments[RabbitMQ.Client.Headers.XExpires] = settings.QueueExpiration.Value.Ticks / TimeSpan.TicksPerMillisecond;

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
