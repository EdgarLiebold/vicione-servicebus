using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a connection context supervisor implementation.
/// </summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly IServiceBusTopologyConfiguration _topologyConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    public ConnectionContextSupervisor(IServiceBusHostConfiguration hostConfiguration, IServiceBusTopologyConfiguration topologyConfiguration)
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
        return new ServiceBusEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>
    /// Creates publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext receiveEndpointContext, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        IServiceBusMessagePublishTopologyConfigurator<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        var settings = publishTopology.GetSendSettings();

        return CreateSendTransportAsync(publishAddress, settings, receiveEndpointContext);
    }

    /// <summary>
    /// Creates send transport.
    /// </summary>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext receiveEndpointContext, Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new ServiceBusEndpointAddress(_hostConfiguration.HostAddress, address);

        var settings = _topologyConfiguration.Send.GetSendSettings(endpointAddress);

        return CreateSendTransportAsync(endpointAddress, settings, receiveEndpointContext);
    }

    /// <summary>
    /// Creates client context supervisor.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <returns>The result of the operation.</returns>
    public IClientContextSupervisor CreateClientContextSupervisor(Func<IConnectionContextSupervisor, IPipeContextFactory<ClientContext>> factory)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var clientContextSupervisor = new ClientContextSupervisor(factory(this));

        AddConsumeAgent(clientContextSupervisor);

        return clientContextSupervisor;
    }

    /// <summary>
    /// Creates send endpoint context supervisor.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
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
