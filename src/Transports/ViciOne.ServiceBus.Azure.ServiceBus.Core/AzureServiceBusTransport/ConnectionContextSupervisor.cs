using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;
using ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly IServiceBusTopologyConfiguration _topologyConfiguration;

    public ConnectionContextSupervisor(IServiceBusHostConfiguration hostConfiguration, IServiceBusTopologyConfiguration topologyConfiguration)
        : base(new ConnectionContextFactory(hostConfiguration))
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    public Uri NormalizeAddress(Uri address)
    {
        return new ServiceBusEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    public Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext receiveEndpointContext, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        IServiceBusMessagePublishTopologyConfigurator<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        var settings = publishTopology.GetSendSettings();

        return CreateSendTransportAsync(publishAddress, settings, receiveEndpointContext);
    }

    public Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext receiveEndpointContext, Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new ServiceBusEndpointAddress(_hostConfiguration.HostAddress, address);

        var settings = _topologyConfiguration.Send.GetSendSettings(endpointAddress);

        return CreateSendTransportAsync(endpointAddress, settings, receiveEndpointContext);
    }

    public IClientContextSupervisor CreateClientContextSupervisor(Func<IConnectionContextSupervisor, IPipeContextFactory<ClientContext>> factory)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var clientContextSupervisor = new ClientContextSupervisor(factory(this));

        AddConsumeAgent(clientContextSupervisor);

        return clientContextSupervisor;
    }

    public ISendEndpointContextSupervisor CreateSendEndpointContextSupervisor(SendSettings settings)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configureTopology = new ConfigureServiceBusTopologyFilter<SendSettings>(settings, settings.GetBrokerTopology(), false);

        var contextFactory = new SendEndpointContextFactory(this, configureTopology, settings);

        return new SendEndpointContextSupervisor(contextFactory);
    }

    Task<ISendTransport> CreateSendTransportAsync(Uri address, SendSettings settings, ReceiveEndpointContext receiveEndpointContext)
    {
        TransportLogMessages.CreateSendTransport(address);

        var supervisor = CreateSendEndpointContextSupervisor(settings);

        var transportContext = new ServiceBusSendTransportContext(_hostConfiguration, receiveEndpointContext, supervisor, settings);

        var transport = new SendTransport<SendEndpointContext>(transportContext);

        AddSendAgent(transport);

        return Task.FromResult<ISendTransport>(transport);
    }
}
