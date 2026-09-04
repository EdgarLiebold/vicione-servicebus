using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.Middleware;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a connection context supervisor implementation.
/// </summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly ISqlTopologyConfiguration _topologyConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    /// <param name="connectionContextFactory">The connection context factory value.</param>
    public ConnectionContextSupervisor(ISqlHostConfiguration hostConfiguration, ISqlTopologyConfiguration topologyConfiguration,
        IPipeContextFactory<ConnectionContext> connectionContextFactory)
        : base(connectionContextFactory)
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
        return new SqlEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>
    /// Creates publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(SqlReceiveEndpointContext context, Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        ISqlMessagePublishTopologyConfigurator<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        var settings = publishTopology.GetSendSettings(_hostConfiguration.HostAddress);

        var brokerTopology = publishTopology.GetBrokerTopology();

        IPipe<ClientContext> configureTopology = new ConfigureSqlTopologyFilter<SendSettings>(settings, brokerTopology).ToPipe();

        var supervisor = new ClientContextSupervisor(context.ClientContextSupervisor);

        return CreateSendTransportAsync(publishAddress!,
            new TopicSendTransportContext(_hostConfiguration, context, supervisor, configureTopology, settings.EntityName));
    }

    /// <summary>
    /// Creates send transport.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendTransport> CreateSendTransportAsync(SqlReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new SqlEndpointAddress(_hostConfiguration.HostAddress, address);

        var settings = _topologyConfiguration.Send.GetSendSettings(endpointAddress);

        IPipe<ClientContext> configureTopology = new ConfigureSqlTopologyFilter<SendSettings>(settings, settings.GetBrokerTopology()).ToPipe();

        var supervisor = new ClientContextSupervisor(context.ClientContextSupervisor);

        return CreateSendTransportAsync(endpointAddress, endpointAddress.Type == SqlEndpointAddress.AddressType.Queue
            ? new QueueSendTransportContext(_hostConfiguration, context, supervisor, configureTopology, settings.EntityName)
            : new TopicSendTransportContext(_hostConfiguration, context, supervisor, configureTopology, settings.EntityName));
    }

    Task<ISendTransport> CreateSendTransportAsync(Uri address, SendTransportContext<ClientContext> transportContext)
    {
        TransportLogMessages.CreateSendTransport(address);

        var transport = new SendTransport<ClientContext>(transportContext);

        AddSendAgent(transport);

        return Task.FromResult<ISendTransport>(transport);
    }
}
