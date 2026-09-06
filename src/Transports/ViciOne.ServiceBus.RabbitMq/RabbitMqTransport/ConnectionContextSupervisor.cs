using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Supervises RabbitMQ connection contexts and creates send transports from shared channels.</summary>
public class ConnectionContextSupervisor :
    TransportPipeContextSupervisor<ConnectionContext>,
    IConnectionContextSupervisor
{
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly IRabbitMqTopologyConfiguration _topologyConfiguration;

    /// <summary>Creates a connection supervisor from host and topology configuration.</summary>
    /// <param name="hostConfiguration">The RabbitMQ connection configuration.</param>
    /// <param name="topologyConfiguration">The RabbitMQ send and publish topology.</param>
    public ConnectionContextSupervisor(IRabbitMqHostConfiguration hostConfiguration, IRabbitMqTopologyConfiguration topologyConfiguration)
        : base(new ConnectionContextFactory(hostConfiguration))
    {
        _hostConfiguration = hostConfiguration;
        _topologyConfiguration = topologyConfiguration;
    }

    /// <summary>Resolves a full or short destination address against the configured host.</summary>
    /// <param name="address">The destination address to resolve.</param>
    /// <returns>The full RabbitMQ destination URI.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return new RabbitMqEndpointAddress(_hostConfiguration.HostAddress, address);
    }

    /// <summary>Creates a send transport and the destination topology it deploys.</summary>
    /// <param name="receiveEndpointContext">The endpoint context that owns the transport.</param>
    /// <param name="channelContextSupervisor">The shared RabbitMQ channel supervisor.</param>
    /// <param name="address">The full or short destination address.</param>
    /// <param name="cancellationToken">Cancellation checked before transport creation.</param>
    /// <returns>The configured send transport.</returns>
    public Task<ISendTransport> CreateSendTransportAsync(RabbitMqReceiveEndpointContext receiveEndpointContext,
        IChannelContextSupervisor channelContextSupervisor, Uri address, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var endpointAddress = new RabbitMqEndpointAddress(_hostConfiguration.HostAddress, address);

        TransportLogMessages.CreateSendTransport(endpointAddress);

        var settings = _topologyConfiguration.Send.GetSendSettings(endpointAddress);

        var brokerTopology = settings.GetBrokerTopology();

        var configureTopology = new ConfigureRabbitMqTopologyFilter<SendSettings>(settings, brokerTopology);

        return CreateSendTransportAsync(receiveEndpointContext, channelContextSupervisor, configureTopology, settings.ExchangeName, endpointAddress);
    }

    /// <summary>Creates the publish transport and topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="receiveEndpointContext">The endpoint context that owns the transport.</param>
    /// <param name="channelContextSupervisor">The shared RabbitMQ channel supervisor.</param>
    /// <param name="cancellationToken">Cancellation checked before transport creation.</param>
    /// <returns>The configured publish transport.</returns>
    public Task<ISendTransport> CreatePublishTransportAsync<T>(RabbitMqReceiveEndpointContext receiveEndpointContext,
        IChannelContextSupervisor channelContextSupervisor, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Transports.ISendTransport>(cancellationToken); LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        IRabbitMqMessagePublishTopology<T> publishTopology = _topologyConfiguration.Publish.GetMessageTopology<T>();

        var settings = publishTopology.GetSendSettings(_hostConfiguration.HostAddress);

        var brokerTopology = publishTopology.GetBrokerTopology();

        var configureTopology = new ConfigureRabbitMqTopologyFilter<SendSettings>(settings, brokerTopology);

        var endpointAddress = settings.GetSendAddress(_hostConfiguration.HostAddress);

        return CreateSendTransportAsync(receiveEndpointContext, channelContextSupervisor, configureTopology, publishTopology.Exchange.ExchangeName,
            endpointAddress);
    }

    Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor,
        ConfigureRabbitMqTopologyFilter<SendSettings> filter, string exchangeName, RabbitMqEndpointAddress endpointAddress)
    {
        var supervisor = new ChannelContextSupervisor(channelContextSupervisor);

        var delaySettings = endpointAddress.GetDelaySettings();

        IPipe<ChannelContext> delayPipe = new ConfigureRabbitMqTopologyFilter<DelaySettings>(delaySettings, delaySettings.GetBrokerTopology()).ToPipe();

        var sendTransportContext = new RabbitMqSendTransportContext(_hostConfiguration, receiveEndpointContext, supervisor, filter, exchangeName,
            delayPipe, delaySettings.ExchangeName);

        var transport = new SendTransport<ChannelContext>(sendTransportContext);

        channelContextSupervisor.AddSendAgent(transport);

        return Task.FromResult<ISendTransport>(transport);
    }
}
