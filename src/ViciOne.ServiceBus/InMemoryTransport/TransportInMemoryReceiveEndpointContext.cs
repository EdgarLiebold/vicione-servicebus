using System;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Carries state for transport in memory receive endpoint operations.</summary>
public class TransportInMemoryReceiveEndpointContext :
    BaseReceiveEndpointContext,
    InMemoryReceiveEndpointContext
{
    readonly IInMemoryReceiveEndpointConfiguration _configuration;
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="configuration">The callback used to configure the component.</param>
    public TransportInMemoryReceiveEndpointContext(IInMemoryHostConfiguration hostConfiguration, IInMemoryReceiveEndpointConfiguration configuration)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>Gets the send.</summary>
    public ISendTopology Send => _configuration.Topology.Send;
    /// <summary>Gets the message fabric.</summary>
    public IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric => _hostConfiguration.TransportProvider.MessageFabric;
    /// <summary>Gets the transport context.</summary>
    public InMemoryTransportContext TransportContext => _hostConfiguration.TransportProvider;

    /// <summary>Adds send agent to the configuration.</summary>
    /// <param name="agent">The agent.</param>
    public override void AddSendAgent(IAgent agent)
    {
        throw new NotSupportedException();
    }

    /// <summary>Adds consume agent to the configuration.</summary>
    /// <param name="agent">The agent.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        throw new NotSupportedException();
    }

    /// <summary>Converts exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <returns>The converted exception.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return exception;
    }

    /// <summary>Configures topology.</summary>
    public void ConfigureTopology()
    {
        var builder = new MessageFabricConsumeTopologyBuilder<InMemoryTransportContext, InMemoryTransportMessage>(_hostConfiguration.TransportProvider,
            MessageFabric);

        var name = _configuration.InputAddress.GetEndpointName()
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "The in-memory input address must contain an endpoint name.", "Correct the named configuration before starting the host"));

        builder.Exchange = name;
        builder.ExchangeDeclare(name, ExchangeType.FanOut);

        builder.Queue = name;
        builder.QueueDeclare(name);

        builder.QueueBind(builder.Exchange, builder.Queue);

        _configuration.Topology.Consume.Apply(builder);
    }

    /// <summary>Creates send transport provider.</summary>
    /// <returns>The created send transport provider.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new InMemorySendTransportProvider(_hostConfiguration.TransportProvider, this);
    }

    /// <summary>Creates publish transport provider.</summary>
    /// <returns>The created publish transport provider.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new InMemoryPublishTransportProvider(_hostConfiguration.TransportProvider, this);
    }
}
