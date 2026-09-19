using System;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Provides the transport services and topology state for an in-memory receive endpoint.</summary>
internal sealed class InMemoryReceiveEndpointContext :
    BaseReceiveEndpointContext,
    IInMemoryReceiveEndpointContext
{
    readonly IInMemoryReceiveEndpointConfiguration _configuration;
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>Creates runtime context from host and endpoint configuration.</summary>
    /// <param name="hostConfiguration">The host that owns the message fabric.</param>
    /// <param name="configuration">The receive endpoint configuration.</param>
    public InMemoryReceiveEndpointContext(IInMemoryHostConfiguration hostConfiguration, IInMemoryReceiveEndpointConfiguration configuration)
        : base(
            hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration)),
            configuration ?? throw new ArgumentNullException(nameof(configuration)))
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>Gets the endpoint's send topology.</summary>
    public ISendTopology Send => _configuration.Topology.Send;
    /// <summary>Gets the host's shared message fabric.</summary>
    public IMessageFabric<InMemoryTransportMessage> MessageFabric => _hostConfiguration.TransportProvider.MessageFabric;
    /// <summary>Gets the host-specific context that isolates fabric entities.</summary>
    public IInMemoryTransportContext TransportContext => _hostConfiguration.TransportProvider;
    /// <summary>Gets the payload-admission policy attached to the owning in-memory host.</summary>
    public IPayloadAdmissionRuntime? PayloadAdmissionRuntime =>
        (_hostConfiguration as IPayloadAdmissionHostConfiguration)?.PayloadAdmissionRuntime;

    /// <summary>Rejects send agents because the in-memory fabric owns send lifecycle directly.</summary>
    /// <param name="agent">The unsupported send agent.</param>
    public override void AddSendAgent(IAgent agent)
    {
        throw new NotSupportedException("The in-memory receive endpoint does not own separate send agents.");
    }

    /// <summary>Rejects consume agents because the receive transport owns its consumer agent.</summary>
    /// <param name="agent">The unsupported consume agent.</param>
    public override void AddConsumeAgent(IAgent agent)
    {
        throw new NotSupportedException("The in-memory receive endpoint does not own separate consume agents.");
    }

    /// <summary>Returns the original exception because the in-memory transport has no provider exception wrapper.</summary>
    /// <param name="exception">The transport exception.</param>
    /// <param name="message">The contextual error message, unused by this transport.</param>
    /// <returns>The original exception instance.</returns>
    public override Exception ConvertException(Exception exception, string message)
    {
        return exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>Declares the endpoint queue and exchange before applying consume bindings.</summary>
    public void ConfigureTopology()
    {
        var name = _configuration.InputAddress.GetEndpointName()
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "The in-memory input address must contain an endpoint name.", "Correct the named configuration before starting the host"));

        var builder = new MessageFabricConsumeTopologyBuilder<InMemoryTransportMessage>(MessageFabric, name, name);

        builder.ExchangeDeclare(name, InMemoryExchangeType.FanOut);

        builder.QueueDeclare(name);

        builder.QueueBind(builder.Exchange, builder.Queue);

        _configuration.Topology.Consume.Apply(builder);
    }

    /// <summary>Creates the endpoint's point-to-point transport resolver.</summary>
    /// <returns>The send transport resolver.</returns>
    protected override ISendTransportProvider CreateSendTransportProvider()
    {
        return new InMemorySendTransportProvider(_hostConfiguration.TransportProvider, this);
    }

    /// <summary>Creates the endpoint's publish transport resolver.</summary>
    /// <returns>The publish transport resolver.</returns>
    protected override IPublishTransportProvider CreatePublishTransportProvider()
    {
        return new InMemoryPublishTransportProvider(_hostConfiguration.TransportProvider, this);
    }
}
