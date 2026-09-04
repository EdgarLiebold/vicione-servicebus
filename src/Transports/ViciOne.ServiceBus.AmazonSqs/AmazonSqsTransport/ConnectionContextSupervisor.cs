using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a connection context supervisor implementation.
/// </summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly IAmazonSqsTopologyConfiguration _topologyConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    public ConnectionContextSupervisor(IAmazonSqsHostConfiguration hostConfiguration, IAmazonSqsTopologyConfiguration topologyConfiguration)
        : base(new ConnectionContextFactory(hostConfiguration))
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    /// <summary>
    /// Performs the normalize address operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return new AmazonSqsEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>
    /// Creates send transport.
    /// </summary>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="clientContextSupervisor">The client context supervisor value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="clientContextSupervisor">The client context supervisor value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
