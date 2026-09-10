using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Owns configuration and topology for one named in-memory receive endpoint.</summary>
internal sealed class InMemoryReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration,
    IInMemoryReceiveEndpointConfiguration,
    IInMemoryReceiveEndpointConfigurator
{
    readonly IInMemoryEndpointConfiguration _endpointConfiguration;
    readonly IInMemoryHostConfiguration _hostConfiguration;
    readonly string _queueName;

    /// <summary>Creates a receive endpoint configuration for a named queue.</summary>
    /// <param name="hostConfiguration">The host that owns the endpoint.</param>
    /// <param name="queueName">The non-empty queue name.</param>
    /// <param name="endpointConfiguration">The inherited pipeline and topology configuration.</param>
    public InMemoryReceiveEndpointConfiguration(IInMemoryHostConfiguration hostConfiguration, string queueName,
        IInMemoryEndpointConfiguration endpointConfiguration)
        : base(
            hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration)),
            endpointConfiguration ?? throw new ArgumentNullException(nameof(endpointConfiguration)))
    {
        _hostConfiguration = hostConfiguration;

        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        _queueName = queueName;
        _endpointConfiguration = endpointConfiguration;

        HostAddress = hostConfiguration.HostAddress;

        InputAddress = new InMemoryEndpointAddress(hostConfiguration.HostAddress, queueName);

        Receive.Configurator.AddPipeSpecification(new InMemoryDurableSendCompletionPipeSpecification());
    }

    IInMemoryReceiveEndpointConfigurator IInMemoryReceiveEndpointConfiguration.Configurator => this;

    IInMemoryTopologyConfiguration IInMemoryEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>Gets the owning host's loopback address.</summary>
    public override Uri HostAddress { get; }

    /// <summary>Gets the endpoint queue's canonical input address.</summary>
    public override Uri InputAddress { get; }

    /// <summary>Materializes the receive pipeline into a runtime endpoint context.</summary>
    /// <returns>The configured runtime endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateInMemoryReceiveEndpointContext();
    }

    /// <summary>Builds and registers the receive endpoint with its host.</summary>
    /// <param name="host">The host that owns the endpoint lifecycle.</param>
    public void Build(IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        var context = CreateInMemoryReceiveEndpointContext();

        var transport = new InMemoryReceiveTransport(context, _queueName);

        var receiveEndpoint = new ReceiveEndpoint(transport, context);

        host.AddReceiveEndpoint(_queueName, receiveEndpoint);

        ReceiveEndpoint = receiveEndpoint;
    }

    /// <summary>Binds a named exchange to this endpoint's queue.</summary>
    /// <param name="exchangeName">The non-empty source exchange name.</param>
    /// <param name="exchangeType">The source exchange routing behavior.</param>
    /// <param name="routingKey">The optional direct or topic routing key.</param>
    public void Bind(string exchangeName, ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchangeName);

        _endpointConfiguration.Topology.Consume.Bind(exchangeName, exchangeType, routingKey);
    }

    /// <summary>Binds the exchange for a message contract to this endpoint's queue.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="exchangeType">The source exchange routing behavior.</param>
    /// <param name="routingKey">The optional direct or topic routing key.</param>
    public void Bind<TMessage>(ExchangeType exchangeType, string? routingKey = default)
        where TMessage : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<TMessage>().Bind(exchangeType, routingKey);
    }

    IInMemoryReceiveEndpointContext CreateInMemoryReceiveEndpointContext()
    {
        var builder = new InMemoryReceiveEndpointBuilder(_hostConfiguration, this);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }
}
