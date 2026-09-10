using System;
using System.Collections.Generic;
using System.Data;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Owns SQL transport host settings, topology, endpoint registrations, and connection supervision.</summary>
public class SqlHostConfiguration :
    BaseHostConfiguration<ISqlReceiveEndpointConfiguration, ISqlReceiveEndpointConfigurator>,
    ISqlHostConfiguration
{
    readonly ISqlBusConfiguration _busConfiguration;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly ISqlBusTopology _topology;
    SqlHostSettings? _hostSettings;

    /// <summary>Creates a SQL transport host configuration with retry and recyclable connection supervision.</summary>
    /// <param name="busConfiguration">The bus configuration that creates endpoint-level configuration.</param>
    /// <param name="topologyConfiguration">The topology configuration shared by endpoints and database connections.</param>
    public SqlHostConfiguration(ISqlBusConfiguration busConfiguration, ISqlTopologyConfiguration topologyConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;

        _topology = new SqlBusTopology(this, topologyConfiguration);

        ReceiveTransportRetryPolicy = Retry.CreatePolicy(x =>
        {
            x.Handle<ConnectionException>();
            x.Handle<DBConcurrencyException>();

            x.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        });

        _connectionContext = new Recycle<IConnectionContextSupervisor>(() =>
            new ConnectionContextSupervisor(this, topologyConfiguration, _hostSettings!.CreateConnectionContextFactory(this)));
    }

    /// <summary>Gets the recyclable supervisor for the shared database connection context.</summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    /// <summary>Gets the address of the configured SQL transport host.</summary>
    public override Uri HostAddress => _hostSettings?.HostAddress ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "The host was not configured.", "Correct the named configuration before starting the host"));

    ISqlBusTopology ISqlHostConfiguration.Topology => _topology;

    /// <summary>Gets the policy used to retry transient receive-transport connection failures.</summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>Gets the SQL transport bus topology.</summary>
    public override IBusTopology Topology => _topology;

    /// <summary>Gets or sets the required SQL transport host settings.</summary>
    public SqlHostSettings Settings
    {
        get => _hostSettings ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("SQL transport", "unknown", "The host was not configured.", "Correct the named configuration before starting the host"));
        set => _hostSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Applies common endpoint settings and assigns the standard idle expiry to temporary SQL queues.</summary>
    /// <param name="configurator">The SQL receive endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition whose settings are applied.</param>
    public void ApplyEndpointDefinition(ISqlReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
            configurator.AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle;

        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>Creates and registers a SQL-backed receive endpoint with a new endpoint configuration.</summary>
    /// <param name="queueName">The logical SQL queue name.</param>
    /// <param name="configure">An optional callback applied before observers are notified and the endpoint is registered.</param>
    /// <returns>The registered SQL receive endpoint configuration.</returns>
    public ISqlReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<ISqlReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SqlReceiveSettings(endpointConfiguration, queueName);

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Creates and registers a SQL-backed receive endpoint from explicit queue and endpoint settings.</summary>
    /// <param name="settings">The SQL queue settings used by the receive endpoint.</param>
    /// <param name="endpointConfiguration">The shared endpoint pipeline and serialization configuration.</param>
    /// <param name="configure">An optional callback applied before observers are notified and the endpoint is registered.</param>
    /// <returns>The registered SQL receive endpoint configuration.</returns>
    public ISqlReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(SqlReceiveSettings settings,
        ISqlEndpointConfiguration endpointConfiguration, Action<ISqlReceiveEndpointConfigurator>? configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (endpointConfiguration == null)
            throw new ArgumentNullException(nameof(endpointConfiguration));

        var configuration = new SqlReceiveEndpointConfiguration(this, settings, endpointConfiguration);

        configure?.Invoke(configuration);

        Observers.EndpointConfigured(configuration);

        Add(configuration);

        return configuration;
    }

    /// <summary>Registers a SQL-backed receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that applies SQL-transport-specific settings after the definition.</param>
    public override void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<ISqlReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        ReceiveEndpoint(queueName, configurator =>
        {
            ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Creates and registers a SQL-backed receive endpoint for a named queue.</summary>
    /// <param name="queueName">The logical SQL queue name.</param>
    /// <param name="configureEndpoint">The callback applied before the endpoint is registered.</param>
    public override void ReceiveEndpoint(string queueName, Action<ISqlReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Validates the host settings and yields all inherited endpoint failures.</summary>
    /// <returns>The failures that prevent the SQL transport host from being built.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        if (_hostSettings == null)
            yield return this.Failure("Host", "Database must be configured");
        else
        {
            foreach (var result in _hostSettings.Validate())
                yield return result;

        }

        foreach (var result in base.Validate())
            yield return result;
    }

    /// <summary>Creates a SQL endpoint while exposing it through the provider-neutral host contract.</summary>
    /// <param name="queueName">The logical SQL queue name.</param>
    /// <param name="configure">An optional provider-neutral callback adapted to the SQL configurator.</param>
    /// <returns>The registered SQL receive endpoint configuration.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IReceiveEndpointConfigurator>? configure)
    {
        Action<ISqlReceiveEndpointConfigurator>? configureEndpoint = configure == null
            ? null
            : endpoint => configure(endpoint);

        return CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Builds the SQL transport host and attaches every registered receive endpoint.</summary>
    /// <returns>The configured SQL transport host.</returns>
    public override IHost Build()
    {
        var host = new SqlHost(this, _topology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
