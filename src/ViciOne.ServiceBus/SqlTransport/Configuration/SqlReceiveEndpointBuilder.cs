using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Builds sql receive endpoint components.</summary>
public class SqlReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly ISqlReceiveEndpointConfiguration _configuration;
    readonly ISqlHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="configuration">The callback used to configure the component.</param>
    public SqlReceiveEndpointBuilder(ISqlHostConfiguration hostConfiguration, ISqlReceiveEndpointConfiguration configuration)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public override ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
    {
        if (_configuration.ConfigureConsumeTopology && options.HasFlag(ConnectPipeOptions.ConfigureConsumeTopology))
        {
            ISqlMessageConsumeTopologyConfigurator<T> topology = _configuration.Topology.Consume.GetMessageTopology<T>();
            if (topology.ConfigureConsumeTopology)
                topology.Subscribe();
        }

        return base.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Creates receive endpoint context.</summary>
    /// <returns>The created receive endpoint context.</returns>
    public SqlReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var brokerTopology = BuildTopology(_configuration.Settings);

        var deadLetterTransport = CreateDeadLetterTransport();
        var errorTransport = CreateErrorTransport();

        var context = new QueueSqlReceiveEndpointContext(_hostConfiguration, _configuration, brokerTopology);

        context.GetOrAddPayload(() => deadLetterTransport);
        context.GetOrAddPayload(() => errorTransport);
        context.GetOrAddPayload(() => _hostConfiguration.Topology);

        return context;
    }

    IErrorTransport CreateErrorTransport()
    {
        return new SqlQueueErrorTransport(_configuration.Settings.QueueName, SqlQueueType.ErrorQueue);
    }

    IDeadLetterTransport CreateDeadLetterTransport()
    {
        return new SqlQueueDeadLetterTransport(_configuration.Settings.QueueName, SqlQueueType.DeadLetterQueue);
    }

    BrokerTopology BuildTopology(ReceiveSettings settings)
    {
        var topologyBuilder = new ReceiveEndpointBrokerTopologyBuilder(settings);

        _configuration.Topology.Consume.Apply(topologyBuilder);

        return topologyBuilder.BuildBrokerTopology();
    }
}
