using System;
using System.Collections.Generic;
using System.Data;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.SqlTransport.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public class SqlHostConfiguration :
    BaseHostConfiguration<ISqlReceiveEndpointConfiguration, ISqlReceiveEndpointConfigurator>,
    ISqlHostConfiguration
{
    readonly ISqlBusConfiguration _busConfiguration;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly ISqlBusTopology _topology;
    SqlHostSettings? _hostSettings;

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

    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    public override Uri HostAddress => _hostSettings?.HostAddress ?? throw new ConfigurationException("The host was not configured.");

    ISqlBusTopology ISqlHostConfiguration.Topology => _topology;

    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    public override IBusTopology Topology => _topology;

    public SqlHostSettings Settings
    {
        get => _hostSettings ?? throw new ConfigurationException("The host was not configured.");
        set => _hostSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void ApplyEndpointDefinition(ISqlReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
            configurator.AutoDeleteOnIdle = Defaults.TemporaryAutoDeleteOnIdle;

        base.ApplyEndpointDefinition(configurator, definition);
    }

    public ISqlReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<ISqlReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new SqlReceiveSettings(endpointConfiguration, queueName);

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

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

    public override void ReceiveEndpoint(string queueName, Action<ISqlReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

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

    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IReceiveEndpointConfigurator>? configure)
    {
        return CreateReceiveEndpointConfiguration(queueName, configure);
    }

    public override IHost Build()
    {
        var host = new SqlHost(this, _topology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
