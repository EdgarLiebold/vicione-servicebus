using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Supervises Amazon connection contexts and creates address-specific send transports.</summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly IAmazonSqsTopologyConfiguration _topologyConfiguration;

    /// <summary>Initializes an Amazon connection-context supervisor.</summary>
    /// <param name="hostConfiguration">The host configuration used by connection and transport contexts.</param>
    /// <param name="topologyConfiguration">The topology used to resolve send and publish entities.</param>
    public ConnectionContextSupervisor(IAmazonSqsHostConfiguration hostConfiguration, IAmazonSqsTopologyConfiguration topologyConfiguration)
        : base(new ConnectionContextFactory(hostConfiguration))
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    /// <summary>Resolves an endpoint address relative to the configured Amazon SQS host.</summary>
    /// <param name="address">The absolute or relative endpoint address.</param>
    /// <returns>The normalized absolute endpoint address.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return new AmazonSqsEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>Creates an Amazon SQS queue transport or Amazon SNS topic transport for an endpoint address.</summary>
    /// <param name="receiveEndpointContext">The receive endpoint requesting the transport.</param>
    /// <param name="clientContextSupervisor">The client supervisor that owns the transport.</param>
    /// <param name="address">The queue or topic endpoint address.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>The created send transport.</returns>
    public Task<ISendTransport> CreateSendTransportAsync(SqsReceiveEndpointContext receiveEndpointContext, IClientContextSupervisor clientContextSupervisor,
        Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new AmazonSqsEndpointAddress(_hostConfiguration.HostAddress, address);

        TransportLogMessages.CreateSendTransport(endpointAddress);

        if (endpointAddress.Type == AmazonSqsEndpointAddress.AddressType.Queue)
        {
            var settings = _topologyConfiguration.Send.GetSendSettings(endpointAddress);

            IPipe<ClientContext> configureTopology = new ConfigureAmazonSqsTopologyFilter<EntitySettings>(settings, settings.GetBrokerTopology()).ToPipe();

            var supervisor = new ClientContextSupervisor(clientContextSupervisor);

            var context = new QueueSendTransportContext(_hostConfiguration, receiveEndpointContext, supervisor, configureTopology, settings.EntityName);

            return CreateTransportAsync(clientContextSupervisor, context);
        }
        else
        {
            var settings = new TopicPublishSettings(endpointAddress);

            var builder = new PublishEndpointBrokerTopologyBuilder();
            var topicHandle = builder.CreateTopic(settings.EntityName, settings.Durable, settings.AutoDelete, settings.TopicAttributes, settings
                .TopicSubscriptionAttributes, settings.Tags);

            builder.Topic ??= topicHandle;

            IPipe<ClientContext> configureTopology = new ConfigureAmazonSqsTopologyFilter<EntitySettings>(settings, builder.BuildBrokerTopology()).ToPipe();

            var supervisor = new ClientContextSupervisor(clientContextSupervisor);

            var context = new TopicSendTransportContext(_hostConfiguration, receiveEndpointContext, supervisor, configureTopology, settings.EntityName);

            return CreateTransportAsync(clientContextSupervisor, context);
        }
    }

    /// <summary>Creates an Amazon SNS publish transport for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <param name="receiveEndpointContext">The receive endpoint requesting the transport.</param>
    /// <param name="clientContextSupervisor">The client supervisor that owns the transport.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>The created publish transport.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(SqsReceiveEndpointContext receiveEndpointContext,
        IClientContextSupervisor clientContextSupervisor, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        IAmazonSqsMessagePublishTopology<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        var settings = publishTopology.GetPublishSettings(_hostConfiguration.HostAddress);

        IPipe<ClientContext> configureTopology =
            new ConfigureAmazonSqsTopologyFilter<EntitySettings>(settings, publishTopology.GetBrokerTopology()).ToPipe();

        var supervisor = new ClientContextSupervisor(clientContextSupervisor);

        var context = new TopicSendTransportContext(_hostConfiguration, receiveEndpointContext, supervisor, configureTopology, settings.EntityName);

        return CreateTransportAsync(clientContextSupervisor, context);
    }

    static Task<ISendTransport> CreateTransportAsync(IClientContextSupervisor clientContextSupervisor, SendTransportContext<ClientContext> transportContext)
    {
        var transport = new SendTransport<ClientContext>(transportContext);

        clientContextSupervisor.AddSendAgent(transport);

        return Task.FromResult<ISendTransport>(transport);
    }
}
