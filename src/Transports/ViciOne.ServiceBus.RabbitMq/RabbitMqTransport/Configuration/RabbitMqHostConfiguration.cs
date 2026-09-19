using System;
using System.Collections.Generic;
using System.IO;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RabbitMq.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Owns RabbitMQ host settings, topology, endpoint registrations, and connection supervision.</summary>
public class RabbitMqHostConfiguration :
    BaseHostConfiguration<IRabbitMqReceiveEndpointConfiguration, IRabbitMqReceiveEndpointConfigurator>,
    IRabbitMqHostConfiguration
{
    readonly IRabbitMqBusConfiguration _busConfiguration;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly IRabbitMqBusTopology _topology;
    HostAddressSnapshot? _builtAddress;
    RabbitMqHostSettings _hostSettings;

    /// <summary>Creates a RabbitMQ host configuration with secure defaults, transport retry, and recyclable connection supervision.</summary>
    /// <param name="busConfiguration">The bus configuration that creates endpoint-level configuration.</param>
    /// <param name="topologyConfiguration">The topology configuration shared by endpoints and broker connections.</param>
    public RabbitMqHostConfiguration(IRabbitMqBusConfiguration busConfiguration, IRabbitMqTopologyConfiguration topologyConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostSettings = new ConfigurationHostSettings
        {
            Host = "localhost",
            VirtualHost = "/",
            Port = 5672,
            Username = "guest",
            Password = "guest"
        };

        var messageNameFormatter = new RabbitMqMessageNameFormatter();

        _topology = new RabbitMqBusTopology(this, messageNameFormatter, topologyConfiguration);

        ReceiveTransportRetryPolicy = Retry.CreatePolicy(x =>
        {
            // Exclusive-resource conflicts are terminal because retrying cannot acquire a queue owned
            // by another connection. The client may surface that refusal directly, after transport
            // conversion, or through an already-closed channel, so every carrier applies the same rule.
            x.Handle<ConnectionException>(exception => !exception.IsExclusiveResourceConflict());
            x.Handle<AlreadyClosedException>(exception => !exception.IsExclusiveResourceConflict());
            x.Handle<EndOfStreamException>();
            x.Handle<OperationInterruptedException>(exception =>
                exception.ChannelShouldBeClosed() && !exception.IsExclusiveResourceConflict());
            x.Handle<NotSupportedException>(exception => exception.Message.Contains("Pipelining of requests forbidden"));

            x.Ignore<AuthenticationFailureException>();
            // A configuration failure can retain a broker reply as its inner exception.
            x.Ignore<ConfigurationException>();

            x.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        });

        _connectionContext = new Recycle<IConnectionContextSupervisor>(() => new ConnectionContextSupervisor(this, topologyConfiguration));
    }

    /// <summary>Gets the recyclable supervisor for the shared RabbitMQ connection context.</summary>
    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    /// <summary>Gets the address of the configured RabbitMQ host.</summary>
    public override Uri HostAddress
    {
        get
        {
            RabbitMqHostSettings settings = Settings;
            return _builtAddress?.Address ?? settings.HostAddress;
        }
    }

    /// <summary>Gets whether published messages require broker confirmation.</summary>
    public bool PublisherConfirmation => _hostSettings.PublisherConfirmation;

    /// <summary>Gets the current RabbitMQ publish-batch limits.</summary>
    public BatchSettings BatchSettings => _hostSettings.BatchSettings;

    IRabbitMqBusTopology IRabbitMqHostConfiguration.Topology => _topology;

    /// <summary>Gets the policy used to retry transient receive-transport connection failures.</summary>
    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    /// <summary>Gets the RabbitMQ bus topology.</summary>
    public override IBusTopology Topology => _topology;

    /// <summary>Gets or sets the RabbitMQ host settings.</summary>
    public RabbitMqHostSettings Settings
    {
        get
        {
            if (_builtAddress is { } builtAddress && !builtAddress.Matches(_hostSettings))
                throw new InvalidOperationException("RabbitMQ host address settings cannot change after the host is built.");

            return _hostSettings;
        }
        set
        {
            if (_builtAddress is not null)
                throw new InvalidOperationException("RabbitMQ host settings cannot be replaced after the host is built.");

            _hostSettings = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>Applies common endpoint settings and maps temporary endpoints to expiring, non-durable RabbitMQ queues.</summary>
    /// <param name="configurator">The RabbitMQ receive endpoint configurator to update.</param>
    /// <param name="definition">The endpoint definition whose settings are applied.</param>
    public void ApplyEndpointDefinition(IRabbitMqReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
        {
            configurator.QueueExpiration = TimeSpan.FromMinutes(1);
            configurator.AutoDelete = true;
            configurator.Durable = false;
        }

        base.ApplyEndpointDefinition(configurator, definition);
    }

    /// <summary>Creates and registers a durable RabbitMQ queue endpoint using the configured default exchange type.</summary>
    /// <param name="queueName">The RabbitMQ queue name.</param>
    /// <param name="configure">An optional callback applied before observers are notified and the endpoint is registered.</param>
    /// <returns>The registered RabbitMQ receive endpoint configuration.</returns>
    public IRabbitMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IRabbitMqReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new RabbitMqReceiveSettings(endpointConfiguration, queueName,
            _busConfiguration.Topology.Consume.ExchangeTypeSelector.DefaultExchangeType, true, false);

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    /// <summary>Creates and registers a RabbitMQ receive endpoint from explicit queue and endpoint settings.</summary>
    /// <param name="settings">The RabbitMQ queue and exchange settings used by the receive endpoint.</param>
    /// <param name="endpointConfiguration">The shared endpoint pipeline and serialization configuration.</param>
    /// <param name="configure">An optional callback applied before observers are notified and the endpoint is registered.</param>
    /// <returns>The registered RabbitMQ receive endpoint configuration.</returns>
    public IRabbitMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(RabbitMqReceiveSettings settings,
        IRabbitMqEndpointConfiguration endpointConfiguration, Action<IRabbitMqReceiveEndpointConfigurator>? configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (endpointConfiguration == null)
            throw new ArgumentNullException(nameof(endpointConfiguration));

        var configuration = new RabbitMqReceiveEndpointConfiguration(this, settings, endpointConfiguration);

        configure?.Invoke(configuration);

        Observers.EndpointConfigured(configuration);

        Add(configuration);

        return configuration;
    }

    /// <summary>Registers a RabbitMQ receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that applies RabbitMQ-specific settings after the definition.</param>
    public override void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IRabbitMqReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        ReceiveEndpoint(queueName, configurator =>
        {
            ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Creates and registers a RabbitMQ receive endpoint for a named queue.</summary>
    /// <param name="queueName">The RabbitMQ queue name.</param>
    /// <param name="configureEndpoint">The callback applied before the endpoint is registered.</param>
    public override void ReceiveEndpoint(string queueName, Action<IRabbitMqReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Validates inherited endpoint settings and RabbitMQ batch timeout, message-count, and byte-size limits.</summary>
    /// <returns>The failures that prevent the RabbitMQ host from being built.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in base.Validate())
            yield return result;

        if (_hostSettings.BatchSettings.Enabled)
        {
            if (_hostSettings.BatchSettings.Timeout < TimeSpan.Zero || _hostSettings.BatchSettings.Timeout > TimeSpan.FromSeconds(1))
                yield return this.Failure("BatchTimeout", "must be >= 0 and <= 1s");

            if (_hostSettings.BatchSettings.MessageLimit <= 1 || _hostSettings.BatchSettings.MessageLimit > 100)
                yield return this.Failure("BatchMessageLimit", "must be > 1 and <= 100");

            if (_hostSettings.BatchSettings.SizeLimit < 1024 || _hostSettings.BatchSettings.SizeLimit > 256 * 1024)
                yield return this.Failure("BatchSizeLimit", "must be >= 1K and <= 256K");
        }
    }

    /// <summary>Creates a RabbitMQ endpoint while exposing it through the provider-neutral host contract.</summary>
    /// <param name="queueName">The RabbitMQ queue name.</param>
    /// <param name="configure">An optional provider-neutral callback adapted to the RabbitMQ configurator.</param>
    /// <returns>The registered RabbitMQ receive endpoint configuration.</returns>
    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IReceiveEndpointConfigurator>? configure = null)
    {
        Action<IRabbitMqReceiveEndpointConfigurator>? configureEndpoint = configure == null
            ? null
            : endpoint => configure(endpoint);

        return CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    /// <summary>Builds the RabbitMQ host and attaches every registered receive endpoint.</summary>
    /// <returns>The configured RabbitMQ host.</returns>
    public override IHost Build()
    {
        RabbitMqHostSettings settings = Settings;
        var host = new RabbitMqHost(this, _topology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        _builtAddress = new HostAddressSnapshot(
            settings.HostAddress, settings.Host, settings.Port, settings.VirtualHost, settings.Ssl);
        if (settings is ConfigurationHostSettings configurationSettings)
            configurationSettings.FreezeAddress();

        return host;
    }

    readonly record struct HostAddressSnapshot(Uri Address, string? Host, int Port, string? VirtualHost, bool Ssl)
    {
        public bool Matches(RabbitMqHostSettings settings)
        {
            return Address.Equals(settings.HostAddress)
                && string.Equals(Host, settings.Host, StringComparison.Ordinal)
                && Port == settings.Port
                && string.Equals(VirtualHost, settings.VirtualHost, StringComparison.Ordinal)
                && Ssl == settings.Ssl;
        }
    }
}
