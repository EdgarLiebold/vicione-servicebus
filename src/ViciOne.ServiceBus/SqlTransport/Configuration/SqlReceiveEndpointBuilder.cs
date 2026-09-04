using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql receive endpoint builder implementation.
/// </summary>
public class SqlReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly ISqlReceiveEndpointConfiguration _configuration;
    readonly ISqlHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    public SqlReceiveEndpointBuilder(ISqlHostConfiguration hostConfiguration, ISqlReceiveEndpointConfiguration configuration)
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
            ISqlMessageConsumeTopologyConfigurator<T> topology = _configuration.Topology.Consume.GetMessageTopology<T>();
            if (topology.ConfigureConsumeTopology)
                topology.Subscribe();
        }

        return base.ConnectConsumePipe(pipe, options);
    }

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
