using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.SqlTransport.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Owns the shared SQL connection context and creates endpoint-scoped send transports.</summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly ISqlHostConfiguration _hostConfiguration;
    readonly ISqlTopologyConfiguration _topologyConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="topologyConfiguration">The topology configuration.</param>
    /// <param name="connectionContextFactory">The connection context factory.</param>
    public ConnectionContextSupervisor(ISqlHostConfiguration hostConfiguration, ISqlTopologyConfiguration topologyConfiguration,
        IPipeContextFactory<ConnectionContext> connectionContextFactory)
        : base(connectionContextFactory)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        _topologyConfiguration = topologyConfiguration ?? throw new ArgumentNullException(nameof(topologyConfiguration));
    }

    /// <summary>Normalizes address.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The uri produced by the operation.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return new SqlEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>Creates publish transport.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="publishAddress">The publish address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(SqlReceiveEndpointContext context, Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(publishAddress);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendTransport>(cancellationToken);

        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        ISqlMessagePublishTopologyConfigurator<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        var settings = publishTopology.GetSendSettings(_hostConfiguration.HostAddress);

        var brokerTopology = publishTopology.GetBrokerTopology();

        IPipe<ClientContext> configureTopology = new ConfigureSqlTopologyFilter<SendSettings>(settings, brokerTopology).ToPipe();

        var supervisor = new ClientContextSupervisor(context.ClientContextSupervisor);

        return CreateSendTransportAsync(publishAddress,
            new TopicSendTransportContext(_hostConfiguration, context, supervisor, configureTopology, settings.EntityName));
    }

    /// <summary>Creates send transport.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    public Task<ISendTransport> CreateSendTransportAsync(SqlReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(address);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<ISendTransport>(cancellationToken);

        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new SqlEndpointAddress(_hostConfiguration.HostAddress, address);

        var settings = _topologyConfiguration.Send.GetSendSettings(endpointAddress);

        IPipe<ClientContext> configureTopology = new ConfigureSqlTopologyFilter<SendSettings>(settings, settings.GetBrokerTopology()).ToPipe();

        var supervisor = new ClientContextSupervisor(context.ClientContextSupervisor);

        return CreateSendTransportAsync(endpointAddress, endpointAddress.Kind == SqlEndpointKind.Queue
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
