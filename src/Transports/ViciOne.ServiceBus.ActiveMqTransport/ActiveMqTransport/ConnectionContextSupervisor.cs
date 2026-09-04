using System;
using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.ActiveMqTransport.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly IActiveMqTopologyConfiguration _topologyConfiguration;

    public ConnectionContextSupervisor(IActiveMqHostConfiguration hostConfiguration, IActiveMqTopologyConfiguration topologyConfiguration)
        : base(new ConnectionContextFactory(hostConfiguration))
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    public Uri NormalizeAddress(Uri address)
    {
        return new ActiveMqEndpointAddress(_hostConfiguration.HostAddress, address);
    }

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
