using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Provides an in memory host configuration implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    /// <param name="baseAddress">The base address value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
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

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress => _hostAddress;
    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public override IBusTopology Topology => _topology;

    /// <summary>
    /// Gets the receive transport retry policy value.
    /// </summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>
    /// Gets or sets the base address value.
    /// </summary>
    public Uri BaseAddress
    {
        set => _hostAddress = value ?? new Uri("loopback://localhost/");
    }

    /// <summary>
    /// Gets or sets the queue capacity value.
    /// </summary>
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

    /// <summary>
    /// Performs the apply endpoint definition operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="definition">The definition value.</param>
    public void ApplyEndpointDefinition(IInMemoryReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IInMemoryReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IInMemoryReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();

        return CreateReceiveEndpointConfiguration(queueName, endpointConfiguration, configure);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
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

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public override void ReceiveEndpoint(string queueName, Action<IInMemoryReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, Action<IReceiveEndpointConfigurator>? configure)
    {
        return CreateReceiveEndpointConfiguration(queueName, configure);
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IHost Build()
    {
        var host = new InMemoryHost(this, _topology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
