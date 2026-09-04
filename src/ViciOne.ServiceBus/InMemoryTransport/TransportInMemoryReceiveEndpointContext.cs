using System;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides a transport in memory receive endpoint context implementation.
/// </summary>
public class TransportInMemoryReceiveEndpointContext :
    BaseReceiveEndpointContext,
    InMemoryReceiveEndpointContext
{
    readonly IInMemoryReceiveEndpointConfiguration _configuration;
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    public TransportInMemoryReceiveEndpointContext(IInMemoryHostConfiguration hostConfiguration, IInMemoryReceiveEndpointConfiguration configuration)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the send value.
    /// </summary>
    public ISendTopology Send => _configuration.Topology.Send;
    /// <summary>
    /// Gets the message fabric value.
    /// </summary>
    public IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric => _hostConfiguration.TransportProvider.MessageFabric;
    /// <summary>
    /// Gets the transport context value.
    /// </summary>
    public InMemoryTransportContext TransportContext => _hostConfiguration.TransportProvider;

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddSendAgent(IAgent agent)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <param name="agent">The agent value.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Performs the convert exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return exception;
    }

    /// <summary>
    /// Configures topology.
    /// </summary>
    public void ConfigureTopology()
    {
        var builder = new MessageFabricConsumeTopologyBuilder<InMemoryTransportContext, InMemoryTransportMessage>(_hostConfiguration.TransportProvider,
            MessageFabric);

        var name = _configuration.InputAddress.GetEndpointName()
            ?? throw new ConfigurationException("The in-memory input address must contain an endpoint name.");

        builder.Exchange = name;
        builder.ExchangeDeclare(name, ExchangeType.FanOut);

        builder.Queue = name;
        builder.QueueDeclare(name);

        builder.QueueBind(builder.Exchange, builder.Queue);

        _configuration.Topology.Consume.Apply(builder);
    }

    /// <summary>
    /// Creates send transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new InMemorySendTransportProvider(_hostConfiguration.TransportProvider, this);
    }

    /// <summary>
    /// Creates publish transport provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new InMemoryPublishTransportProvider(_hostConfiguration.TransportProvider, this);
    }
}
