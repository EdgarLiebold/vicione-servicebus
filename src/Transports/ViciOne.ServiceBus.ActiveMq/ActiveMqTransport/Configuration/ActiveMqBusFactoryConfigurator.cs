using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an active mq bus factory configurator implementation.
/// </summary>
public class ActiveMqBusFactoryConfigurator :
    BusFactoryConfigurator,
    IActiveMqBusFactoryConfigurator,
    IBusFactory
{
    readonly IActiveMqBusConfiguration _busConfiguration;
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly ActiveMqQueueReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="busConfiguration">The bus configuration value.</param>
    public ActiveMqBusFactoryConfigurator(IActiveMqBusConfiguration busConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostConfiguration = busConfiguration.HostConfiguration;

        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");

        _settings = new ActiveMqQueueReceiveSettings(busConfiguration.BusEndpointConfiguration, queueName, false, true);
    }

    /// <summary>
    /// Gets or sets the durable value.
    /// </summary>
    public bool Durable
    {
        set => _settings.Durable = value;
    }

    /// <summary>
    /// Gets or sets the auto delete value.
    /// </summary>
    public bool AutoDelete
    {
        set => _settings.AutoDelete = value;
    }

    /// <summary>
    /// Performs the host operation.
    /// </summary>
    /// <param name="settings">The settings value.</param>
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

    /// <summary>
    /// Publishes a message to its configured consumers.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Publish(Type messageType, Action<IActiveMqMessagePublishTopologyConfigurator>? configure = null)
    {
        var configurator = _busConfiguration.Topology.Publish.GetMessageTopology(messageType);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Gets the send topology value.
    /// </summary>
    public new IActiveMqSendTopologyConfigurator SendTopology => _busConfiguration.Topology.Send;
    /// <summary>
    /// Gets the publish topology value.
    /// </summary>
    public new IActiveMqPublishTopologyConfigurator PublishTopology => _busConfiguration.Topology.Publish;

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IActiveMqReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(string queueName, Action<IActiveMqReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>
    /// Performs the receive endpoint operation.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    public void ReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator> configureEndpoint)
    {
        _hostConfiguration.ReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>
    /// Sets consumer endpoint queue name formatter.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public void SetConsumerEndpointQueueNameFormatter(IActiveMqConsumerEndpointQueueNameFormatter formatter)
    {
        _busConfiguration.Topology.Consume.ConsumerEndpointQueueNameFormatter = formatter;
    }

    /// <summary>
    /// Performs the enable artemis compatibility operation.
    /// </summary>
    public void EnableArtemisCompatibility()
    {
        SetConsumerEndpointQueueNameFormatter(new ArtemisConsumerEndpointQueueNameFormatter());

        _hostConfiguration.IsArtemis = true;
    }

    /// <summary>
    /// Sets temporary queue name formatter.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public void SetTemporaryQueueNameFormatter(IActiveMqTemporaryQueueNameFormatter? formatter)
    {
        _busConfiguration.Topology.Consume.TemporaryQueueNameFormatter = formatter;
    }

    /// <summary>
    /// Sets temporary queue name prefix.
    /// </summary>
    /// <param name="prefix">The prefix value.</param>
    public void SetTemporaryQueueNamePrefix(string prefix)
    {
        SetTemporaryQueueNameFormatter(string.IsNullOrWhiteSpace(prefix) ? null : new PrefixTemporaryQueueNameFormatter(prefix));
    }

    /// <summary>
    /// Creates bus endpoint configuration.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public IReceiveEndpointConfiguration CreateBusEndpointConfiguration(Action<IReceiveEndpointConfigurator> configure)
    {
        var queueName = _busConfiguration.Topology.Consume.CreateTemporaryQueueName("bus");
        var settings = new ActiveMqQueueReceiveSettings(_busConfiguration.BusEndpointConfiguration, queueName, _settings.Durable, _settings.AutoDelete);

        return _busConfiguration.HostConfiguration.CreateReceiveEndpointConfiguration(settings, _busConfiguration.BusEndpointConfiguration, configure);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in base.Validate())
            yield return result;

        if (string.IsNullOrWhiteSpace(_settings.EntityName))
            yield return this.Failure("Bus", "The bus queue name must not be null or empty");
    }
}
