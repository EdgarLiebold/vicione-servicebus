using System;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;

/// <summary>Owns Amazon SQS host settings, topology, endpoint registrations, and client supervision.</summary>
public class AmazonSqsHostConfiguration :
    BaseHostConfiguration<IAmazonSqsReceiveEndpointConfiguration, IAmazonSqsReceiveEndpointConfigurator>,
    IAmazonSqsHostConfiguration
{
    readonly IAmazonSqsBusConfiguration _busConfiguration;
    readonly IAmazonSqsBusTopology _busTopology;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly IAmazonSqsTopologyConfiguration _topologyConfiguration;
    AmazonSqsHostSettings? _hostSettings;

    /// <summary>Creates an Amazon SQS host configuration with transport retry and recyclable client supervision.</summary>
    /// <param name="busConfiguration">The bus configuration that creates endpoint-level configuration.</param>
    /// <param name="topologyConfiguration">The Amazon SQS and SNS topology configuration shared by endpoints and clients.</param>
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

    /// <summary>Gets the recyclable supervisor for the shared Amazon SQS and SNS client context.</summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    /// <summary>Gets the address of the configured Amazon SQS host.</summary>
    public override Uri HostAddress => Settings.HostAddress;

    /// <summary>Gets or sets the required Amazon SQS host settings and applies any configured topic-name scope.</summary>
    public AmazonSqsHostSettings Settings
    {
        get => _hostSettings ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Amazon SQS", "unknown", "The Amazon SQS host must be configured.", "Correct the named configuration before starting the host"));
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

    /// <summary>Gets the Amazon SQS and SNS bus topology.</summary>
    public override IBusTopology Topology => _busTopology;

    /// <summary>Gets the policy used to retry transient Amazon SQS transport and connection failures.</summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>Applies common endpoint settings and maps temporary endpoints to non-durable, auto-deleting SQS queues.</summary>
    /// <param name="configurator">The Amazon SQS receive endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition whose settings are applied.</param>
    public void ApplyEndpointDefinition(IAmazonSqsReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
        {
            configurator.AutoDelete = true;
            configurator.Durable = false;
        }

        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>Creates and registers a durable Amazon SQS queue endpoint configuration.</summary>
    /// <param name="queueName">The Amazon SQS queue name.</param>
    /// <param name="configure">An optional callback applied before observers are notified and the endpoint is registered.</param>
    /// <returns>The registered Amazon SQS receive endpoint configuration.</returns>
    public IAmazonSqsReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();

        var settings = new QueueReceiveSettings(endpointConfiguration, queueName, true, false);

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Creates and registers an Amazon SQS receive endpoint from explicit queue and endpoint settings.</summary>
    /// <param name="settings">The Amazon SQS queue settings used by the receive endpoint.</param>
    /// <param name="endpointConfiguration">The shared endpoint pipeline and serialization configuration.</param>
    /// <param name="configure">An optional callback applied before observers are notified and the endpoint is registered.</param>
    /// <returns>The registered Amazon SQS receive endpoint configuration.</returns>
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

    /// <summary>Registers an Amazon SQS receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that applies Amazon-SQS-specific settings after the definition.</param>
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

    /// <summary>Creates and registers an Amazon SQS receive endpoint for a named queue.</summary>
    /// <param name="queueName">The Amazon SQS queue name.</param>
    /// <param name="configureEndpoint">An optional callback applied before the endpoint is registered.</param>
    public override void ReceiveEndpoint(string queueName, Action<IAmazonSqsReceiveEndpointConfigurator>? configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Creates an Amazon SQS endpoint while exposing it through the provider-neutral host contract.</summary>
    /// <param name="queueName">The Amazon SQS queue name.</param>
    /// <param name="configure">An optional provider-neutral callback adapted to the Amazon SQS configurator.</param>
    /// <returns>The registered Amazon SQS receive endpoint configuration.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName, Action<IReceiveEndpointConfigurator>? configure)
    {
        Action<IAmazonSqsReceiveEndpointConfigurator>? configureEndpoint = configure == null
            ? null
            : endpoint => configure(endpoint);

        return CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Builds the Amazon SQS host and attaches every registered receive endpoint.</summary>
    /// <returns>The configured Amazon SQS host.</returns>
    public override IHost Build()
    {
        var host = new AmazonSqsHost(this, _busTopology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
