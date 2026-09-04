using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq receive endpoint builder implementation.
/// </summary>
public class ActiveMqReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IActiveMqReceiveEndpointConfiguration _configuration;
    readonly IActiveMqHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    public ActiveMqReceiveEndpointBuilder(IActiveMqHostConfiguration hostConfiguration, IActiveMqReceiveEndpointConfiguration configuration)
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
            IActiveMqMessageConsumeTopologyConfigurator<T> topology = _configuration.Topology.Consume.GetMessageTopology<T>();
            if (topology.ConfigureConsumeTopology)
                topology.Bind();
        }

        return base.ConnectConsumePipe(pipe, options);
    }

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ActiveMqReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var brokerTopology = BuildTopology(_configuration.Settings);

        var context = new ActiveMqConsumerReceiveEndpointContext(_hostConfiguration, _configuration, brokerTopology);

        var deadLetterTransport = CreateDeadLetterTransport(context);
        var errorTransport = CreateErrorTransport(context);


        context.GetOrAddPayload(() => deadLetterTransport);
        context.GetOrAddPayload(() => errorTransport);
        context.GetOrAddPayload(() => _hostConfiguration.Topology);

        return context;
    }

    IErrorTransport CreateErrorTransport(ActiveMqReceiveEndpointContext context)
    {
        var settings = _configuration.Topology.Send.GetErrorSettings(_configuration.Settings);
        var filter = new ConfigureActiveMqTopologyFilter<ErrorSettings>(settings, settings.GetBrokerTopology(), context);

        return new ActiveMqErrorTransport(new QueueEntity(0, settings.EntityName, settings.Durable, settings.AutoDelete), filter);
    }

    IDeadLetterTransport CreateDeadLetterTransport(ActiveMqReceiveEndpointContext context)
    {
        var settings = _configuration.Topology.Send.GetDeadLetterSettings(_configuration.Settings);
        var filter = new ConfigureActiveMqTopologyFilter<DeadLetterSettings>(settings, settings.GetBrokerTopology(), context);

        return new ActiveMqDeadLetterTransport(new QueueEntity(0, settings.EntityName, settings.Durable, settings.AutoDelete), filter);
    }

    BrokerTopology BuildTopology(ReceiveSettings settings)
    {
        var topologyBuilder = new ReceiveEndpointBrokerTopologyBuilder();

        topologyBuilder.Queue = topologyBuilder.CreateQueue(settings.EntityName, settings.Durable, settings.AutoDelete);

        _configuration.Topology.Consume.Apply(topologyBuilder);

        return topologyBuilder.BuildTopologyLayout();
    }
}
