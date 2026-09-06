using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Configures an ActiveMQ bus, its host, endpoints, and transport topology.</summary>
public class ActiveMqBusFactoryConfigurator :
    BusFactoryConfigurator,
    IActiveMqBusFactoryConfigurator,
    IBusFactory
{
    readonly IActiveMqBusConfiguration _busConfiguration;
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly ActiveMqQueueReceiveSettings _settings;

    /// <summary>Creates a configurator for an ActiveMQ bus configuration.</summary>
    /// <param name="busConfiguration">The mutable bus configuration.</param>
    public ActiveMqBusFactoryConfigurator(IActiveMqBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");

        _settings = new ActiveMqQueueReceiveSettings(busConfiguration.BusEndpointConfiguration, queueName, false, true);
    }

    /// <summary>Sets whether the bus endpoint queue persists across broker restarts.</summary>
    public bool Durable
    {
        set => _settings.Durable = value;
    }

    /// <summary>Sets whether the broker removes the bus endpoint queue when it is no longer used.</summary>
    public bool AutoDelete
    {
        set => _settings.AutoDelete = value;
    }

    /// <summary>Applies an immutable snapshot of the ActiveMQ host settings.</summary>
    /// <param name="settings">The host settings to snapshot.</param>
    public void Host(ActiveMqHostSettings settings)
    {
        _busConfiguration.HostConfiguration.Settings = new ActiveMqHostSettingsSnapshot(settings);
    }

    void IActiveMqBusFactoryConfigurator.Send<T>(Action<IActiveMqMessageSendTopologyConfigurator<T>> configureTopology)
    {
        IActiveMqMessageSendTopologyConfigurator<T> configurator = _busConfiguration.Topology.Send.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    void IActiveMqBusFactoryConfigurator.Publish<T>(Action<IActiveMqMessagePublishTopologyConfigurator<T>>? configureTopology)
    {
        IActiveMqMessagePublishTopologyConfigurator<T> configurator = _busConfiguration.Topology.Publish.GetMessageTopology<T>();

        configureTopology?.Invoke(configurator);
    }

    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that configures the message's publish topology.</param>
    public void Publish(Type messageType, Action<IActiveMqMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>Gets the ActiveMQ send-topology configurator.</summary>
    public new IActiveMqSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>Gets the ActiveMQ publish-topology configurator.</summary>
    public new IActiveMqPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>Adds an ActiveMQ receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name.</param>
    /// <param name="configureEndpoint">An optional callback that configures ActiveMQ-specific endpoint settings.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IActiveMqReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Adds a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name.</param>
    /// <param name="configureEndpoint">An optional callback that configures transport-independent endpoint settings.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Adds an ActiveMQ receive endpoint for a queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The callback that configures ActiveMQ-specific endpoint settings.</param>
    public void ReceiveEndpoint(string queueName, Action<IActiveMqReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Adds a receive endpoint for an ActiveMQ queue.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The callback that configures transport-independent endpoint settings.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Sets the formatter used for virtual-topic consumer queue names.</summary>
    /// <param name="formatter">The consumer queue-name formatter.</param>
    public void SetConsumerEndpointQueueNameFormatter(IActiveMqConsumerEndpointQueueNameFormatter formatter)
    {
        _busConfiguration.Topology.Consume.ConsumerEndpointQueueNameFormatter = formatter;
    }

    /// <summary>Enables ActiveMQ Artemis naming and AMQP delivery-delay behavior.</summary>
    public void EnableArtemisCompatibility()
    {
        SetConsumerEndpointQueueNameFormatter(new ArtemisConsumerEndpointQueueNameFormatter());

        _hostConfiguration.IsArtemis = true;
    }

    /// <summary>Sets the formatter used for temporary queue names.</summary>
    /// <param name="formatter">The formatter, or <see langword="null" /> to restore generated names.</param>
    public void SetTemporaryQueueNameFormatter(IActiveMqTemporaryQueueNameFormatter? formatter)
    {
        _busConfiguration.Topology.Consume.TemporaryQueueNameFormatter = formatter;
    }

    /// <summary>Sets a prefix for generated temporary queue names.</summary>
    /// <param name="prefix">The prefix, or a blank value to remove the custom formatter.</param>
    public void SetTemporaryQueueNamePrefix(string prefix)
    {
        SetTemporaryQueueNameFormatter(string.IsNullOrWhiteSpace(prefix) ? null : new PrefixTemporaryQueueNameFormatter(prefix));
    }

    /// <summary>Creates a receive-endpoint configuration for the bus endpoint.</summary>
    /// <param name="configure">The callback that configures the bus endpoint.</param>
    /// <returns>The configured bus receive endpoint.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");
        var settings = new ActiveMqQueueReceiveSettings(_busConfiguration.BusEndpointConfiguration, queueName, _settings.Durable, _settings.AutoDelete);

        return _busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(settings, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in base.Validate())
            yield return result;

        if (string.IsNullOrWhiteSpace(_settings.EntityName))
            yield return this.Failure("Bus", "The bus queue name must not be null or empty");
    }
}
