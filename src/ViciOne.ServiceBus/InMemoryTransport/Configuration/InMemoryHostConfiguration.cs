using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Owns in-memory host settings, topology, endpoint registrations, and the shared message fabric.</summary>
public class InMemoryHostConfiguration :
    BaseHostConfiguration<IInMemoryReceiveEndpointConfiguration, IInMemoryReceiveEndpointConfigurator>,
    IInMemoryHostConfiguration,
    IInMemoryHostConfigurator
{
    readonly IInMemoryBusConfiguration _busConfiguration;
    readonly InMemoryBusTopology _topology;
    readonly Recycle<IInMemoryTransportProvider> _transportProvider;
    Uri _hostAddress;
    int _queueCapacity = 1024;

    /// <summary>Creates an in-memory host configuration and its recyclable transport provider.</summary>
    /// <param name="busConfiguration">The bus configuration that creates endpoint-level configuration.</param>
    /// <param name="baseAddress">The base address for in-memory endpoints, or <see langword="null"/> for <c>loopback://localhost/</c>.</param>
    /// <param name="topologyConfiguration">The topology configuration shared by endpoints and the message fabric.</param>
    public InMemoryHostConfiguration(IInMemoryBusConfiguration busConfiguration, Uri? baseAddress, IInMemoryTopologyConfiguration topologyConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;

        _hostAddress = baseAddress ?? new Uri("loopback://localhost/");
        _topology = new InMemoryBusTopology(this, topologyConfiguration);

        ReceiveTransportRetryPolicy = Retry.CreatePolicy(x =>
        {
            x.Handle<ConnectionException>();

            x.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        });

        _transportProvider = new Recycle<IInMemoryTransportProvider>(() => new InMemoryTransportProvider(this, topologyConfiguration));
    }

    /// <summary>Gets the base address used to resolve in-memory endpoints.</summary>
    public override Uri HostAddress => _hostAddress;
    /// <summary>Gets the in-memory bus topology.</summary>
    public override IBusTopology Topology => _topology;

    /// <summary>Gets the policy used to retry transient receive-transport connection failures.</summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>Sets the base address used to resolve in-memory endpoints, defaulting a null value to <c>loopback://localhost/</c>.</summary>
    public Uri BaseAddress
    {
        set => _hostAddress = value ?? new Uri("loopback://localhost/");
    }

    /// <summary>Sets the positive maximum number of messages buffered by each in-memory queue.</summary>
    public int QueueCapacity
    {
        set => _queueCapacity = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "In-memory queue capacity must be greater than zero.");
    }

    IInMemoryHostConfigurator IInMemoryHostConfiguration.Configurator => this;
    IInMemoryTransportProvider IInMemoryHostConfiguration.TransportProvider => _transportProvider.Supervisor;
    int IInMemoryHostConfiguration.QueueCapacity => _queueCapacity;
    IInMemoryBusTopology IInMemoryHostConfiguration.Topology => _topology;

    /// <summary>Applies the common endpoint-definition settings to an in-memory endpoint.</summary>
    /// <param name="configurator">The in-memory receive endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition whose settings are applied.</param>
    public void ApplyEndpointDefinition(IInMemoryReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>Creates and registers an in-memory receive endpoint with a new endpoint configuration.</summary>
    /// <param name="queueName">The in-memory queue name.</param>
    /// <param name="configure">An optional callback applied before observers are notified and the endpoint is registered.</param>
    /// <returns>The registered in-memory receive endpoint configuration.</returns>
    public IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IInMemoryReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();

        return CreateReceiveEndpointConfiguration(queueName, endpointConfiguration, configure);
    }

    /// <summary>Creates and registers an in-memory receive endpoint from an existing endpoint configuration.</summary>
    /// <param name="queueName">The non-empty in-memory queue name.</param>
    /// <param name="endpointConfiguration">The shared endpoint pipeline and serialization configuration.</param>
    /// <param name="configure">An optional callback applied before observers are notified and the endpoint is registered.</param>
    /// <returns>The registered in-memory receive endpoint configuration.</returns>
    public IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        IInMemoryEndpointConfiguration endpointConfiguration, Action<IInMemoryReceiveEndpointConfigurator>? configure)
    {
        if (endpointConfiguration == null)
            throw new ArgumentNullException(nameof(endpointConfiguration));
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(queueName));

        var configuration = new InMemoryReceiveEndpointConfiguration(this, queueName, endpointConfiguration);

        configure?.Invoke(configuration);

        Observers.EndpointConfigured(configuration);
        Add(configuration);

        return configuration;
    }

    /// <summary>Registers an in-memory receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that applies in-memory-specific settings after the definition.</param>
    public override void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IInMemoryReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        ReceiveEndpoint(queueName, configurator =>
        {
            ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Creates and registers an in-memory receive endpoint for a named queue.</summary>
    /// <param name="queueName">The in-memory queue name.</param>
    /// <param name="configureEndpoint">The callback applied before the endpoint is registered.</param>
    public override void ReceiveEndpoint(string queueName, Action<IInMemoryReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Creates an in-memory endpoint while exposing it through the provider-neutral host contract.</summary>
    /// <param name="queueName">The in-memory queue name.</param>
    /// <param name="configure">An optional provider-neutral callback adapted to the in-memory configurator.</param>
    /// <returns>The registered in-memory receive endpoint configuration.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, Action<IReceiveEndpointConfigurator>? configure)
    {
        Action<IInMemoryReceiveEndpointConfigurator>? configureEndpoint = configure == null
            ? null
            : endpoint => configure(endpoint);

        return CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Builds the in-memory host and attaches every registered receive endpoint.</summary>
    /// <returns>The configured in-memory host.</returns>
    public override IHost Build()
    {
        var host = new InMemoryHost(this, _topology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
