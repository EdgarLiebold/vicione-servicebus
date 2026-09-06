using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Stores and validates in memory receive endpoint configuration.</summary>
public class InMemoryReceiveEndpointConfiguration :
    ReceiveEndpointConfiguration,
    IInMemoryReceiveEndpointConfiguration,
    IInMemoryReceiveEndpointConfigurator
{
    readonly IInMemoryEndpointConfiguration _endpointConfiguration;
    readonly IInMemoryHostConfiguration _hostConfiguration;
    readonly string _queueName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
    public InMemoryReceiveEndpointConfiguration(IInMemoryHostConfiguration hostConfiguration, string queueName,
        IInMemoryEndpointConfiguration endpointConfiguration)
        : base(hostConfiguration, endpointConfiguration)
    {
        _hostConfiguration = hostConfiguration;

        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        _endpointConfiguration = endpointConfiguration ?? throw new ArgumentNullException(nameof(endpointConfiguration));

        HostAddress = hostConfiguration?.HostAddress ?? throw new ArgumentNullException(nameof(hostConfiguration.HostAddress));

        InputAddress = new InMemoryEndpointAddress(hostConfiguration.HostAddress, queueName);

        Receive.Configurator.AddPipeSpecification(new InMemoryDurableSendCompletionPipeSpecification());
    }

    IInMemoryReceiveEndpointConfigurator IInMemoryReceiveEndpointConfiguration.Configurator => this;

    IInMemoryTopologyConfiguration IInMemoryEndpointConfiguration.Topology => _endpointConfiguration.Topology;

    /// <summary>Gets the host address.</summary>
    public override Uri HostAddress { get; }

    /// <summary>Gets the input address.</summary>
    public override Uri InputAddress { get; }

    /// <summary>Creates receive endpoint context.</summary>
    /// <returns>The created receive endpoint context.</returns>
    public override ReceiveEndpointContext CreateReceiveEndpointContext()
    {
        return CreateInMemoryReceiveEndpointContext();
    }

    /// <summary>Builds the configured component.</summary>
    /// <param name="host">The host.</param>
    public void Build(IHost host)
    {
        var context = CreateInMemoryReceiveEndpointContext();

        var transport = new InMemoryReceiveTransport(context, _queueName);

        var receiveEndpoint = new ReceiveEndpoint(transport, context);

        host.AddReceiveEndpoint(_queueName, receiveEndpoint);

        ReceiveEndpoint = receiveEndpoint;
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public void Bind(string exchangeName, ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default)
    {
        if (exchangeName == null)
            throw new ArgumentNullException(nameof(exchangeName));

        _endpointConfiguration.Topology.Consume.Bind(exchangeName, exchangeType, routingKey);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public void Bind<T>(ExchangeType exchangeType, string? routingKey = default)
        where T : class
    {
        _endpointConfiguration.Topology.Consume.GetMessageTopology<T>().Bind(exchangeType, routingKey);
    }

    InMemoryReceiveEndpointContext CreateInMemoryReceiveEndpointContext()
    {
        var builder = new InMemoryReceiveEndpointBuilder(_hostConfiguration, this);

        ApplySpecifications(builder);

        return builder.CreateReceiveEndpointContext();
    }
}
