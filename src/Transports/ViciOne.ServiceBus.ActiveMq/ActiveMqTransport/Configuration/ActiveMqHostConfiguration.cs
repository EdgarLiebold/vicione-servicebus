using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Owns ActiveMQ host settings, topology, connection supervision, and receive endpoints.</summary>
public class ActiveMqHostConfiguration :
    BaseHostConfiguration<IActiveMqReceiveEndpointConfiguration, IActiveMqReceiveEndpointConfigurator>,
    IActiveMqHostConfiguration
{
    readonly IActiveMqBusConfiguration _busConfiguration;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly IActiveMqBusTopology _topology;
    ActiveMqHostSettings? _hostSettings;

    /// <summary>Creates an ActiveMQ host configuration for a bus.</summary>
    /// <param name="busConfiguration">The owning bus configuration.</param>
    /// <param name="topologyConfiguration">The ActiveMQ topology configuration.</param>
    public ActiveMqHostConfiguration(IActiveMqBusConfiguration busConfiguration, IActiveMqTopologyConfiguration topologyConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;

        _topology = new ActiveMqBusTopology(this, topologyConfiguration);

        ReceiveTransportRetryPolicy = Retry.CreatePolicy(x =>
        {
            x.Handle<ConnectionException>();
            x.Handle<NMSException>();
            x.Handle<ChannelClosedException>();

            x.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        });

        _connectionContext = new Recycle<IConnectionContextSupervisor>(() => new ConnectionContextSupervisor(this, topologyConfiguration));
    }

    /// <summary>Gets the configured ActiveMQ broker address.</summary>
    public override Uri HostAddress => Settings.HostAddress;

    /// <summary>Gets the retry policy for transient ActiveMQ receive failures.</summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>Gets or sets the required ActiveMQ host settings.</summary>
    public ActiveMqHostSettings Settings
    {
        get => _hostSettings ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("ActiveMQ", "unknown", "The ActiveMQ host was not configured.", "Correct the named configuration before starting the host"));
        set => _hostSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets whether delayed delivery uses ActiveMQ Artemis scheduling headers.</summary>
    public bool IsArtemis { get; set; }

    /// <summary>Gets the recyclable broker connection supervisor.</summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    IActiveMqBusTopology IActiveMqHostConfiguration.Topology => _topology;

    /// <summary>Applies an endpoint definition, including temporary-queue semantics.</summary>
    /// <param name="configurator">The ActiveMQ endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition to apply.</param>
    public void ApplyEndpointDefinition(IActiveMqReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
        {
            configurator.AutoDelete = true;
            configurator.Durable = false;
        }

        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>Creates and registers a durable ActiveMQ receive-endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">An optional callback that configures the endpoint.</param>
    /// <returns>The registered receive-endpoint configuration.</returns>
    public IActiveMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IActiveMqReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();

        var settings = new ActiveMqQueueReceiveSettings(endpointConfiguration, queueName, true, false);
        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Creates, observes, and registers a receive endpoint from explicit queue settings.</summary>
    /// <param name="settings">The queue receive settings.</param>
    /// <param name="endpointConfiguration">The endpoint's shared configuration.</param>
    /// <param name="configure">An optional callback that configures the ActiveMQ endpoint.</param>
    /// <returns>The registered receive-endpoint configuration.</returns>
    public IActiveMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(ActiveMqQueueReceiveSettings settings,
        IActiveMqEndpointConfiguration endpointConfiguration, Action<IActiveMqReceiveEndpointConfigurator>? configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (endpointConfiguration == null)
            throw new ArgumentNullException(nameof(endpointConfiguration));
        var configuration = new ActiveMqReceiveEndpointConfiguration(this, settings, endpointConfiguration);
        configure?.Invoke(configuration);
        Observers.EndpointConfigured(configuration);
        Add(configuration);
        return configuration;
    }

    /// <summary>Gets the ActiveMQ bus topology.</summary>
    public override IBusTopology Topology => _topology;

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        if (_hostSettings == null)
            yield return this.Failure("Host", "ActiveMQ host must be configured explicitly");

        foreach (var result in base.Validate())
            yield return result;
    }

    /// <summary>Adds a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name.</param>
    /// <param name="configureEndpoint">An optional callback that configures the ActiveMQ endpoint.</param>
    public override void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IActiveMqReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        ReceiveEndpoint(queueName, configurator =>
        {
            ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Adds a receive endpoint for an ActiveMQ queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">An optional callback that configures the ActiveMQ endpoint.</param>
    public override void ReceiveEndpoint(string queueName, Action<IActiveMqReceiveEndpointConfigurator>? configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Creates and registers a transport-independent receive-endpoint configuration.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">An optional callback that configures the endpoint.</param>
    /// <returns>The registered receive-endpoint configuration.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IReceiveEndpointConfigurator>? configure = null)
    {
        Action<IActiveMqReceiveEndpointConfigurator>? configureEndpoint = configure == null
            ? null
            : endpoint => configure(endpoint);

        return CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Builds the ActiveMQ host and all registered receive endpoints.</summary>
    /// <returns>The configured ActiveMQ host.</returns>
    public override IHost Build()
    {
        var host = new ActiveMqHost(this, _topology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
