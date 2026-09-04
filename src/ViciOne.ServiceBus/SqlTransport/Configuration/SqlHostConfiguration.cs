using System;
using System.Collections.Generic;
using System.Data;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql host configuration implementation.
/// </summary>
public class SqlHostConfiguration :
    BaseHostConfiguration<ISqlReceiveEndpointConfiguration, ISqlReceiveEndpointConfigurator>,
    ISqlHostConfiguration
{
    readonly ISqlBusConfiguration _busConfiguration;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly ISqlBusTopology _topology;
    SqlHostSettings? _hostSettings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    /// <param name="topologyConfiguration">The topology configuration value.</param>
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

    /// <summary>
    /// Gets the connection context supervisor value.
    /// </summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public override Uri HostAddress => _hostSettings?.HostAddress ?? throw new ConfigurationException("The host was not configured.");

    ISqlBusTopology ISqlHostConfiguration.Topology => _topology;

    /// <summary>
    /// Gets the receive transport retry policy value.
    /// </summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public override IBusTopology Topology => _topology;

    /// <summary>
    /// Gets or sets the settings value.
    /// </summary>
    public SqlHostSettings Settings
    {
        get => _hostSettings ?? throw new ConfigurationException("The host was not configured.");
        set => _hostSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Performs the apply endpoint definition operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="definition">The definition value.</param>
    public void ApplyEndpointDefinition(ISqlReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
            configurator.AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle;

        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public ISqlReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<ISqlReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SqlReceiveSettings(endpointConfiguration, queueName);

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
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

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public override void ReceiveEndpoint(string queueName, Action<ISqlReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates receive endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IReceiveEndpointConfigurator>? configure)
    {
        return CreateReceiveEndpointConfiguration(queueName, configure);
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IHost Build()
    {
        var host = new SqlHost(this, _topology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
