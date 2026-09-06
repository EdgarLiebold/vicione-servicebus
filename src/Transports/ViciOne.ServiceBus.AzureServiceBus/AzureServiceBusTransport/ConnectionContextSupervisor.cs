using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Supervises the shared namespace connection and transports created from it.</summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly IServiceBusTopologyConfiguration _topologyConfiguration;

    /// <summary>Initializes a namespace connection supervisor.</summary>
    /// <param name="hostConfiguration">The host configuration used to create connections and retry policies.</param>
    /// <param name="topologyConfiguration">The topology used to resolve send and publish settings.</param>
    public ConnectionContextSupervisor(IServiceBusHostConfiguration hostConfiguration, IServiceBusTopologyConfiguration topologyConfiguration)
        : base(new ConnectionContextFactory(hostConfiguration))
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    /// <summary>Resolves an address against the configured Azure Service Bus namespace.</summary>
    /// <param name="address">The relative or absolute endpoint address.</param>
    /// <returns>The normalized transport address.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return new ServiceBusEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>Creates a supervised send transport using a message type's publish topology.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <param name="receiveEndpointContext">The receive context that owns the transport.</param>
    /// <param name="publishAddress">The topic address.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>A task that produces the send transport.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext receiveEndpointContext, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        IServiceBusMessagePublishTopologyConfigurator<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        var settings = publishTopology.GetSendSettings();

        return CreateSendTransportAsync(publishAddress, settings, receiveEndpointContext);
    }

    /// <summary>Creates a supervised send transport for an explicit queue or topic address.</summary>
    /// <param name="receiveEndpointContext">The receive context that owns the transport.</param>
    /// <param name="address">The destination address to normalize.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>A task that produces the send transport.</returns>
    public Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext receiveEndpointContext, Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new ServiceBusEndpointAddress(_hostConfiguration.HostAddress, address);

        var settings = _topologyConfiguration.Send.GetSendSettings(endpointAddress);

        return CreateSendTransportAsync(endpointAddress, settings, receiveEndpointContext);
    }

    /// <summary>Creates and registers a client-context supervisor as a consume agent.</summary>
    /// <param name="factory">The factory that binds a client context to this connection supervisor.</param>
    /// <returns>The registered client-context supervisor.</returns>
    public IClientContextSupervisor CreateClientContextSupervisor(Func<IConnectionContextSupervisor, IPipeContextFactory<ClientContext>> factory)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var clientContextSupervisor = new ClientContextSupervisor(factory(this));

        AddConsumeAgent(clientContextSupervisor);

        return clientContextSupervisor;
    }

    /// <summary>Creates a send-endpoint supervisor whose context configures the destination topology.</summary>
    /// <param name="settings">The destination entity and topology settings.</param>
    /// <returns>The send-endpoint context supervisor.</returns>
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
