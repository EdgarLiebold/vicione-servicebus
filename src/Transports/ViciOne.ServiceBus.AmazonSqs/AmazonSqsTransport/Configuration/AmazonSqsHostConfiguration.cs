using System;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>
/// Provides an amazon sqs host configuration implementation.
/// </summary>
public class AmazonSqsHostConfiguration :
    BaseHostConfiguration<IAmazonSqsReceiveEndpointConfiguration, IAmazonSqsReceiveEndpointConfigurator>,
    IAmazonSqsHostConfiguration
{
    readonly IAmazonSqsBusConfiguration _busConfiguration;
    readonly IAmazonSqsBusTopology _busTopology;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly IAmazonSqsTopologyConfiguration _topologyConfiguration;
    AmazonSqsHostSettings? _hostSettings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
    public AmazonSqsHostConfiguration(IAmazonSqsBusConfiguration busConfiguration, IAmazonSqsTopologyConfiguration
        topologyConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _topologyConfiguration = topologyConfiguration;

        var messageNameFormatter = new AmazonSqsMessageNameFormatter();

        _busTopology = new AmazonSqsBusTopology(this, messageNameFormatter, topologyConfiguration);

        ReceiveTransportRetryPolicy = Retry.CreatePolicy(x =>
        {
            x.Handle<AmazonSqsTransportException>();
            x.Handle<AmazonSqsConnectionException>();

            x.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        });

        _connectionContext = new Recycle<IConnectionContextSupervisor>(() => new ConnectionContextSupervisor(this, topologyConfiguration));
    }

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress => Settings.HostAddress;

    /// <summary>
    /// Gets or sets the settings value.
    /// </summary>
    public AmazonSqsHostSettings Settings
    {
        get => _hostSettings ?? throw new ConfigurationException("The Amazon SQS host must be configured.");
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _hostSettings = value;

            var hostAddress = new AmazonSqsHostAddress(value.HostAddress);

            if (value.ScopeTopics && hostAddress.Scope != "/")
            {
                var formatter = new PrefixEntityNameFormatter(_topologyConfiguration.Message.EntityNameFormatter, hostAddress.Scope.Trim('/') + "_");

                _topologyConfiguration.Message.SetEntityNameFormatter(formatter);
            }
        }
    }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public override IBusTopology Topology => _busTopology;

    /// <summary>
    /// Gets the receive transport retry policy value.
    /// </summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>
    /// Performs the apply endpoint definition operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="definition">The definition value.</param>
    public void ApplyEndpointDefinition(IAmazonSqsReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
        {
            configurator.AutoDelete = true;
            configurator.Durable = false;
        }

        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IAmazonSqsReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();

        var settings = new QueueReceiveSettings(endpointConfiguration, queueName, true, false);

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IAmazonSqsReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(QueueReceiveSettings settings,
        IAmazonSqsEndpointConfiguration endpointConfiguration, Action<IAmazonSqsReceiveEndpointConfigurator>? configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (endpointConfiguration == null)
            throw new ArgumentNullException(nameof(endpointConfiguration));

        var configuration = new AmazonSqsReceiveEndpointConfiguration(this, settings, endpointConfiguration);

        configure?.Invoke(configuration);

        Observers.EndpointConfigured(configuration);

        Add(configuration);

        return configuration;
    }

    IAmazonSqsBusTopology IAmazonSqsHostConfiguration.Topology => _busTopology;

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public override void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configureEndpoint = null)
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
    public override void ReceiveEndpoint(string queueName, Action<IAmazonSqsReceiveEndpointConfigurator>? configureEndpoint)
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
        var host = new AmazonSqsHost(this, _busTopology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
