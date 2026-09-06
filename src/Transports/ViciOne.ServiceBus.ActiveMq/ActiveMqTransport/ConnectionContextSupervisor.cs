using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Supervises ActiveMQ connections and creates destination-specific send transports.</summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly IActiveMqTopologyConfiguration _topologyConfiguration;

    /// <summary>Creates a connection supervisor for an ActiveMQ host and topology.</summary>
    /// <param name="hostConfiguration">The ActiveMQ host configuration.</param>
    /// <param name="topologyConfiguration">The ActiveMQ topology configuration.</param>
    public ConnectionContextSupervisor(IActiveMqHostConfiguration hostConfiguration, IActiveMqTopologyConfiguration topologyConfiguration)
        : base(new ConnectionContextFactory(hostConfiguration))
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    /// <summary>Normalizes a destination address against the configured broker host.</summary>
    /// <param name="address">The queue, topic, or absolute destination address.</param>
    /// <returns>The canonical absolute ActiveMQ destination URI.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return new ActiveMqEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>Creates a send transport for an ActiveMQ queue or topic address.</summary>
    /// <param name="context">The receive-endpoint context that owns the transport.</param>
    /// <param name="sessionContextSupervisor">The parent session supervisor.</param>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>A task that produces the configured send transport.</returns>
    public Task<ISendTransport> CreateSendTransportAsync(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new ActiveMqEndpointAddress(_hostConfiguration.HostAddress, address);

        TransportLogMessages.CreateSendTransport(endpointAddress);

        var settings = _topologyConfiguration.Send.GetSendSettings(endpointAddress);

        IPipe<SessionContext> configureTopology = new ConfigureActiveMqTopologyFilter<SendSettings>(settings, settings.GetBrokerTopology(), context)
            .ToPipe();

        return CreateSendTransportAsync(context, sessionContextSupervisor, configureTopology, settings.EntityName,
            endpointAddress.Type == ActiveMqEndpointAddress.AddressType.Queue ? DestinationType.Queue : DestinationType.Topic);
    }

    /// <summary>Creates a topic send transport from a message type's publish topology.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <param name="context">The receive-endpoint context that owns the transport.</param>
    /// <param name="sessionContextSupervisor">The parent session supervisor.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>A task that produces the configured publish transport.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        IActiveMqMessagePublishTopology<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        var settings = publishTopology.GetSendSettings(_hostConfiguration.HostAddress);

        IPipe<SessionContext> configureTopology = new ConfigureActiveMqTopologyFilter<SendSettings>(settings, publishTopology.GetBrokerTopology(), context)
            .ToPipe();

        return CreateSendTransportAsync(context, sessionContextSupervisor, configureTopology, settings.EntityName, DestinationType.Topic);
    }

    Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor,
        IPipe<SessionContext> pipe, string entityName, DestinationType destinationType)
    {
        var supervisor = new SessionContextSupervisor(sessionContextSupervisor);

        var sendTransportContext = new ActiveMqSendTransportContext(_hostConfiguration, context, supervisor, pipe, entityName, destinationType);

        var transport = new SendTransport<SessionContext>(sendTransportContext);

        sessionContextSupervisor.AddSendAgent(transport);

        return Task.FromResult<ISendTransport>(transport);
    }
}
