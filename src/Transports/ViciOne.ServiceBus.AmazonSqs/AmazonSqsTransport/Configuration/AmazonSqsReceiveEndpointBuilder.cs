using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Builds the topology, context, and move transports for an Amazon SQS receive endpoint.</summary>
public class AmazonSqsReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IAmazonSqsReceiveEndpointConfiguration _configuration;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    /// <summary>Initializes an Amazon SQS receive-endpoint builder.</summary>
    /// <param name="hostConfiguration">The host configuration that supplies transport settings and topology.</param>
    /// <param name="configuration">The receive-endpoint configuration to build.</param>
    public AmazonSqsReceiveEndpointBuilder(IAmazonSqsHostConfiguration hostConfiguration, IAmazonSqsReceiveEndpointConfiguration configuration)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>Connects a consumer pipe and, when requested, subscribes its message type in the consume topology.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">Flags controlling whether consume topology is configured.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public override ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
    {
        if (_configuration.ConfigureConsumeTopology && options.HasFlag(ConnectPipeOptions.ConfigureConsumeTopology))
        {
            IAmazonSqsMessageConsumeTopologyConfigurator<T> topology = _configuration.Topology.Consume.GetMessageTopology<T>();
            if (topology.ConfigureConsumeTopology)
                topology.Subscribe();
        }

        return base.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Builds broker topology and creates the Amazon SQS receive-endpoint context with error and dead-letter transports.</summary>
    /// <returns>The configured receive-endpoint context.</returns>
    public SqsReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var brokerTopology = BuildTopology(_configuration.Settings);

        var headerAdapter = new TransportSetHeaderAdapter<MessageAttributeValue>(
            new SqsHeaderValueConverter(_hostConfiguration.Settings.AllowTransportHeader), TransportHeaderOptions.IncludeFaultMessage);

        var deadLetterTransport = CreateDeadLetterTransport(headerAdapter);

        var errorTransport = CreateErrorTransport(headerAdapter);

        var context = new QueueSqsReceiveEndpointContext(_hostConfiguration, _configuration, brokerTopology);

        context.GetOrAddPayload(() => deadLetterTransport);
        context.GetOrAddPayload(() => errorTransport);
        context.GetOrAddPayload(() => _hostConfiguration.Topology);

        return context;
    }

    BrokerTopology BuildTopology(ReceiveSettings settings)
    {
        var builder = new ReceiveEndpointBrokerTopologyBuilder();

        builder.Queue = builder.CreateQueue(settings.EntityName, settings.Durable, settings.AutoDelete, settings.QueueAttributes,
            settings.QueueSubscriptionAttributes, settings.Tags);

        _configuration.Topology.Consume.Apply(builder);

        return builder.BuildTopologyLayout();
    }

    IErrorTransport CreateErrorTransport(TransportSetHeaderAdapter<MessageAttributeValue> headerAdapter)
    {
        var settings = _configuration.Topology.Send.GetErrorSettings(_configuration.Settings);
        var filter = new ConfigureAmazonSqsTopologyFilter<ErrorSettings>(settings, settings.GetBrokerTopology());

        return new SqsErrorTransport(settings.EntityName, headerAdapter, filter);
    }

    IDeadLetterTransport CreateDeadLetterTransport(TransportSetHeaderAdapter<MessageAttributeValue> headerAdapter)
    {
        var deadLetterSettings = _configuration.Topology.Send.GetDeadLetterSettings(_configuration.Settings);
        var filter = new ConfigureAmazonSqsTopologyFilter<DeadLetterSettings>(deadLetterSettings, deadLetterSettings.GetBrokerTopology());

        return new SqsDeadLetterTransport(deadLetterSettings.EntityName, headerAdapter, filter);
    }
}
